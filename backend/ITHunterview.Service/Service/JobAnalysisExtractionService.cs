using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service
{
    public sealed class JobAnalysisExtractionResult
    {
        public string ProviderName { get; init; } = string.Empty;
        public string RawJson { get; init; } = string.Empty;
        public ValidationResult<ValidatedJobAnalysis> Validation { get; init; } = new();
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

        public JobAnalysisExtractionService(IAiService aiService, IPromptManagementService promptService, IJdAnalysisResponseValidator validator)
        {
            _aiService = aiService;
            _promptService = promptService;
            _validator = validator;
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
            var payload = JsonSerializer.Serialize(input);
            var userPrompt = userPromptTemplate.Replace("[JOB_INPUT_JSON]", payload);
            var provider = await _aiService.GetActiveProviderNameAsync();
            var response = await _aiService.GenerateTextAsync(userPrompt, systemPrompt, provider);
            var rawJson = CleanJsonFence(response);
            return new JobAnalysisExtractionResult
            {
                ProviderName = provider,
                RawJson = rawJson,
                Validation = _validator.Validate(rawJson, input)
            };
        }

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
                schema_version = analysis.SchemaVersion,
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
