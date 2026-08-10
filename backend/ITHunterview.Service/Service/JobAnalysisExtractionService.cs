using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service
{
    public sealed class JobAnalysisExtractionResult
    {
        public string ProviderName { get; init; } = string.Empty;
        public string RawJson { get; init; } = string.Empty;
        public string? PersistableAnalysisJson { get; init; }
        public ValidationResult<ValidatedJobAnalysis> Validation { get; init; } = new();
        public JdAnalysisQuality Quality { get; init; } = JdAnalysisQuality.INVALID;
        public JdAnalysisCoverage Coverage { get; init; } = new(0, 0, 0, 0, 0, 0, false);
        public IReadOnlyList<JdAnalysisDiagnostic> Diagnostics { get; init; } = Array.Empty<JdAnalysisDiagnostic>();
        public string RawTextFallback { get; init; } = string.Empty;
        public int ProviderRequestCount { get; init; }
        public bool UsesRawTextFallback { get; init; }
    }

    public interface IJobAnalysisExtractionService
    {
        Task<JobAnalysisExtractionResult> ExtractAsync(JobAnalysisInputSnapshot input, string systemPrompt, string userPromptTemplate, CancellationToken ct = default);
        Task<JobAnalysisExtractionResult> ExtractWithActivePromptsAsync(JobAnalysisInputSnapshot input, CancellationToken ct = default);
        string SerializeEffectiveAnalysis(
            ValidatedJobAnalysis analysis,
            IReadOnlyCollection<string>? acceptedNormalizedSkillNames = null);
    }

    public sealed class JobAnalysisExtractionService : IJobAnalysisExtractionService
    {
        private readonly IAiService _aiService;
        private readonly IPromptManagementService _promptService;
        private readonly IJdAnalysisResponseValidator _validator;
        private readonly ILogger<JobAnalysisExtractionService> _logger;

        public JobAnalysisExtractionService(
            IAiService aiService,
            IPromptManagementService promptService,
            IJdAnalysisResponseValidator validator,
            ILogger<JobAnalysisExtractionService> logger)
        {
            _aiService = aiService;
            _promptService = promptService;
            _validator = validator;
            _logger = logger;
        }

        public async Task<JobAnalysisExtractionResult> ExtractWithActivePromptsAsync(JobAnalysisInputSnapshot input, CancellationToken ct = default)
        {
            var pair = await _promptService.GetActivePromptPairSnapshotAsync(
                JdAnalysisPromptContract.SystemPromptKey,
                JdAnalysisPromptContract.UserPromptKey,
                ct);
            return await ExtractAsync(input, pair.System.Content, pair.User.Content, ct);
        }

        public async Task<JobAnalysisExtractionResult> ExtractAsync(JobAnalysisInputSnapshot input, string systemPrompt, string userPromptTemplate, CancellationToken ct = default)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));
            var payload = JsonSerializer.Serialize(input);
            var userPrompt = userPromptTemplate.Replace("[JOB_INPUT_JSON]", payload);
            var provider = await _aiService.GetActiveProviderNameAsync();
            var composedSystemPrompt = JdAnalysisOutputSchema.ComposeSystemPrompt(systemPrompt);
            JobAnalysisExtractionResult? best = null;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                string response;
                try
                {
                    response = await _aiService.GenerateTextAsync(
                        userPrompt,
                        composedSystemPrompt,
                        provider,
                        AiGenerationOptions.StrictJsonExtraction,
                        ct,
                        featureCode: "JD_EXTRACTION") ?? string.Empty;
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        "JD analysis provider attempt failed. Provider={Provider}; Attempt={Attempt}; ErrorType={ErrorType}; ErrorCode={ErrorCode}",
                        provider,
                        attempt,
                        exception.GetType().Name,
                        "PROVIDER_REQUEST_FAILED");
                    if (best is not null && best.Quality != JdAnalysisQuality.INVALID)
                    {
                        return CopyWithAttemptCount(best, attempt);
                    }

                    if (attempt < 2)
                    {
                        continue;
                    }

                    return CreateInvalidResult(
                        provider,
                        input,
                        attempt,
                        "PROVIDER_REQUEST_FAILED",
                        new[] { new JdAnalysisDiagnostic("PROVIDER_REQUEST_FAILED", "$") });
                }

                var rawCandidate = CleanJsonFence(response);
                var recovery = JdAnalysisOutputRecovery.Recover(rawCandidate);
                var candidate = BuildCandidate(provider, input, recovery, attempt);
                _logger.LogInformation(
                    "JD analysis attempt completed. Provider={Provider}; Attempt={Attempt}; ResponseLength={ResponseLength}; ResponseHash={ResponseHash}; Quality={Quality}; InputGroupCount={InputGroupCount}; AcceptedGroupCount={AcceptedGroupCount}; DiscardedGroupCount={DiscardedGroupCount}; DiagnosticsCount={DiagnosticsCount}",
                    provider,
                    attempt,
                    response.Length,
                    HashForLog(response),
                    candidate.Quality,
                    candidate.Coverage.InputGroupCount,
                    candidate.Coverage.AcceptedGroupCount,
                    candidate.Coverage.DiscardedGroupCount,
                    candidate.Diagnostics.Count);

                if (IsBetter(candidate, best))
                {
                    best = candidate;
                }

                if (candidate.Quality == JdAnalysisQuality.COMPLETE)
                {
                    return candidate;
                }
            }

            return best is not null
                ? CopyWithAttemptCount(best, 2)
                : CreateInvalidResult(
                    provider,
                    input,
                    2,
                    "INVALID_MODEL_OUTPUT",
                    new[] { new JdAnalysisDiagnostic("INVALID_MODEL_OUTPUT", "$") });
        }

        private JobAnalysisExtractionResult BuildCandidate(
            string provider,
            JobAnalysisInputSnapshot input,
            JdAnalysisRecoveryResult recovery,
            int attempt)
        {
            var persistableJson = recovery.Json;
            ValidationResult<ValidatedJobAnalysis> validation;
            if (string.IsNullOrWhiteSpace(persistableJson))
            {
                validation = new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = false,
                    Quality = JdAnalysisQuality.INVALID,
                    FailureCode = recovery.Diagnostics.FirstOrDefault()?.Code ?? "INVALID_JSON_FORMAT",
                    Errors = recovery.Diagnostics.Select(diagnostic => diagnostic.Code).ToList()
                };
            }
            else
            {
                validation = _validator.Validate(persistableJson, input);
            }

            var diagnostics = new List<JdAnalysisDiagnostic>(recovery.Diagnostics);
            if (validation.Data is not null)
            {
                diagnostics.AddRange(validation.Data.Diagnostics);
            }
            diagnostics = diagnostics
                .GroupBy(diagnostic => $"{diagnostic.Code}:{diagnostic.JsonPath}", StringComparer.Ordinal)
                .Select(group => group.First())
                .Take(100)
                .ToList();

            if (recovery.WasTruncated && validation.Data is not null && validation.Quality != JdAnalysisQuality.INVALID)
            {
                var data = validation.Data;
                var inputGroups = Math.Max(recovery.InputGroupCount, data.RequirementGroups.Count);
                var acceptedGroups = Math.Min(inputGroups, data.RequirementGroups.Count);
                var inputItems = Math.Max(recovery.InputItemCount, data.RequirementGroups.Sum(group => group.Items.Count));
                var acceptedItems = Math.Min(inputItems, data.RequirementGroups.Sum(group => group.Items.Count));
                var coverage = new JdAnalysisCoverage(
                    inputGroups,
                    acceptedGroups,
                    Math.Max(0, inputGroups - acceptedGroups),
                    inputItems,
                    acceptedItems,
                    Math.Max(0, inputItems - acceptedItems),
                    false);
                data.Quality = JdAnalysisQuality.PARTIAL;
                data.Coverage = coverage;
                data.Diagnostics = diagnostics;
                validation.IsValid = false;
                validation.Quality = JdAnalysisQuality.PARTIAL;
                validation.FailureCode = "PARTIAL_JD_ANALYSIS";
                validation.Data = data;
            }

            var quality = validation.Data is not null && validation.Quality != JdAnalysisQuality.INVALID
                ? validation.Quality
                : JdAnalysisQuality.INVALID;
            var coverageResult = validation.Data?.Coverage ?? new JdAnalysisCoverage(0, 0, 0, 0, 0, 0, false);
            var usableJson = validation.Data is not null && quality != JdAnalysisQuality.INVALID
                ? persistableJson
                : null;

            return new JobAnalysisExtractionResult
            {
                ProviderName = provider,
                RawJson = usableJson ?? string.Empty,
                PersistableAnalysisJson = usableJson,
                Validation = validation,
                Quality = quality,
                Coverage = coverageResult,
                Diagnostics = diagnostics,
                RawTextFallback = BuildRawTextFallback(input),
                ProviderRequestCount = attempt,
                UsesRawTextFallback = quality == JdAnalysisQuality.INVALID
            };
        }

        private static bool IsBetter(JobAnalysisExtractionResult candidate, JobAnalysisExtractionResult? current)
        {
            if (current is null) return true;
            var candidateRank = QualityRank(candidate.Quality);
            var currentRank = QualityRank(current.Quality);
            if (candidateRank != currentRank) return candidateRank > currentRank;
            if (candidate.Quality == JdAnalysisQuality.PARTIAL)
            {
                if (candidate.Coverage.AcceptedGroupCount != current.Coverage.AcceptedGroupCount)
                {
                    return candidate.Coverage.AcceptedGroupCount > current.Coverage.AcceptedGroupCount;
                }

                if (candidate.Coverage.AcceptedItemCount != current.Coverage.AcceptedItemCount)
                {
                    return candidate.Coverage.AcceptedItemCount > current.Coverage.AcceptedItemCount;
                }
            }

            return candidate.Diagnostics.Count < current.Diagnostics.Count;
        }

        private static int QualityRank(JdAnalysisQuality quality) => quality switch
        {
            JdAnalysisQuality.COMPLETE => 3,
            JdAnalysisQuality.PARTIAL => 2,
            _ => 1
        };

        private static JobAnalysisExtractionResult CreateInvalidResult(
            string provider,
            JobAnalysisInputSnapshot input,
            int attempts,
            string failureCode,
            IReadOnlyList<JdAnalysisDiagnostic> diagnostics) =>
            new()
            {
                ProviderName = provider,
                RawJson = string.Empty,
                PersistableAnalysisJson = null,
                Validation = new ValidationResult<ValidatedJobAnalysis>
                {
                    IsValid = false,
                    Quality = JdAnalysisQuality.INVALID,
                    FailureCode = failureCode,
                    Errors = diagnostics.Select(diagnostic => diagnostic.Code).ToList()
                },
                Quality = JdAnalysisQuality.INVALID,
                Coverage = new JdAnalysisCoverage(0, 0, 0, 0, 0, 0, false),
                Diagnostics = diagnostics,
                RawTextFallback = BuildRawTextFallback(input),
                ProviderRequestCount = attempts,
                UsesRawTextFallback = true
            };

        private static JobAnalysisExtractionResult CopyWithAttemptCount(
            JobAnalysisExtractionResult source,
            int attemptCount) =>
            new()
            {
                ProviderName = source.ProviderName,
                RawJson = source.RawJson,
                PersistableAnalysisJson = source.PersistableAnalysisJson,
                Validation = source.Validation,
                Quality = source.Quality,
                Coverage = source.Coverage,
                Diagnostics = source.Diagnostics,
                RawTextFallback = source.RawTextFallback,
                ProviderRequestCount = attemptCount,
                UsesRawTextFallback = source.UsesRawTextFallback
            };

        private static string BuildRawTextFallback(JobAnalysisInputSnapshot input)
        {
            var parts = new[] { input.Title, input.Description, input.Requirements }
                .Where(part => !string.IsNullOrWhiteSpace(part))
                .Select(part => part.Trim());
            return string.Join("\n\n", parts);
        }

        private static string HashForLog(string value) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value ?? string.Empty)))[..16].ToLowerInvariant();

        public string SerializeEffectiveAnalysis(
            ValidatedJobAnalysis analysis,
            IReadOnlyCollection<string>? acceptedNormalizedSkillNames = null)
        {
            // requirements_list remains the full, AI-validated matching contract.
            // skills_normalized is deliberately narrower: it contains only skills that
            // were resolved to the active master-skill dictionary and can therefore
            // safely drive tags, filters, and skill-oriented matching.
            var acceptedSkillNames = acceptedNormalizedSkillNames == null
                ? null
                : new HashSet<string>(acceptedNormalizedSkillNames, StringComparer.OrdinalIgnoreCase);

            var effectiveSkills = acceptedSkillNames == null
                ? analysis.SkillsNormalized
                : analysis.SkillsNormalized
                    .Where(s => acceptedSkillNames.Contains(s.Name))
                    .ToList();

            return JsonSerializer.Serialize(new
            {
                // Provider v4 is intentionally compact. Projector, hardcode and
                // Stage 2 consume the stable expanded v3 shape.
                schema_version = JdAnalysisPromptContract.ContractV3,
                analysis_quality = analysis.Quality.ToString(),
                analysis_coverage = new
                {
                    input_group_count = analysis.Coverage.InputGroupCount,
                    accepted_group_count = analysis.Coverage.AcceptedGroupCount,
                    discarded_group_count = analysis.Coverage.DiscardedGroupCount,
                    input_item_count = analysis.Coverage.InputItemCount,
                    accepted_item_count = analysis.Coverage.AcceptedItemCount,
                    discarded_item_count = analysis.Coverage.DiscardedItemCount,
                    requirement_set_complete = analysis.Coverage.RequirementSetComplete
                },
                analysis_diagnostics = analysis.Diagnostics.Take(100).Select(diagnostic => new
                {
                    code = diagnostic.Code,
                    json_path = diagnostic.JsonPath
                }),
                matching_metrics = new
                {
                    job_titles_normalized = analysis.JobTitlesNormalized,
                    skills_normalized = effectiveSkills.ConvertAll(s => new
                    {
                        name = s.Name,
                        category = s.Category,
                        importance = s.Importance,
                        raw_mention = s.RawMention,
                        source_section = s.SourceSection,
                        evidence = s.Evidence,
                        confidence = s.Confidence
                    }),
                    total_years_exp = analysis.TotalYearsExp,
                    domains = analysis.Domains,
                    requirements_list = analysis.RequirementsList.ConvertAll(r => new
                    {
                        category = r.Category,
                        importance = r.Importance,
                        skill_name = r.SkillName,
                        detail_verbatim = r.DetailVerbatim,
                        raw_mention = r.RawMention,
                        source_section = r.SourceSection,
                        evidence = r.Evidence,
                        confidence = r.Confidence
                    }),
                    requirement_groups = analysis.RequirementGroups.ConvertAll(group => new
                    {
                        group_id = group.GroupId,
                        @operator = group.Operator,
                        min_satisfied = group.MinSatisfied,
                        importance = group.Importance,
                        source_section = group.SourceSection,
                        requirement_verbatim = group.RequirementVerbatim,
                        items = group.Items.ConvertAll(item => new
                        {
                            category = item.Category,
                            skill_name = item.SkillName,
                            detail_verbatim = item.DetailVerbatim,
                            raw_mention = item.RawMention,
                            source_section = item.SourceSection,
                            evidences = item.Evidences,
                            min_years = item.MinYears,
                            max_years = item.MaxYears,
                            confidence = item.Confidence
                        })
                    })
                }
            });
        }

        private static string CleanJsonFence(string input)
        {
            var text = (input ?? string.Empty).Trim();
            var match = Regex.Match(text, @"```(?:json)?\s*(.*?)\s*```", RegexOptions.Singleline | RegexOptions.IgnoreCase);
            return match.Success ? match.Groups[1].Value.Trim() : text;
        }
    }
}
