using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service
{
    public interface IJobAnalysisProcessor
    {
        Task ProcessAsync(Guid runId, CancellationToken ct = default);
    }

    public class JobAnalysisProcessor : IJobAnalysisProcessor
    {
        private readonly IJobAnalysisRepository _jobAnalysisRepository;
        private readonly IPromptManagementService _promptService;
        private readonly IJobAnalysisExtractionService _extractionService;
        private readonly ISkillResolver _skillResolver;
        private readonly ILogger<JobAnalysisProcessor> _logger;

        public JobAnalysisProcessor(
            IJobAnalysisRepository jobAnalysisRepository,
            IPromptManagementService promptService,
            IJobAnalysisExtractionService extractionService,
            ISkillResolver skillResolver,
            ILogger<JobAnalysisProcessor> logger)
        {
            _jobAnalysisRepository = jobAnalysisRepository ?? throw new ArgumentNullException(nameof(jobAnalysisRepository));
            _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
            _extractionService = extractionService ?? throw new ArgumentNullException(nameof(extractionService));
            _skillResolver = skillResolver ?? throw new ArgumentNullException(nameof(skillResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessAsync(Guid runId, CancellationToken ct = default)
        {
            var run = await _jobAnalysisRepository.GetRunAsync(runId, ct);
            if (run == null || run.Status != JobAnalysisStatus.PROCESSING)
            {
                _logger.LogWarning("JobAnalysisProcessor: Run {RunId} is null or not in PROCESSING state.", runId);
                return;
            }

            try
            {
                var systemPromptSnapshot = await _promptService.GetPromptSnapshotByVersionIdAsync(run.SystemPromptVersionId, ct);
                var userPromptSnapshot = await _promptService.GetPromptSnapshotByVersionIdAsync(run.UserPromptVersionId, ct);
                JobAnalysisInputSnapshot inputSnapshot;

                try
                {
                    inputSnapshot = JsonSerializer.Deserialize<JobAnalysisInputSnapshot>(run.RawInputSnapshot)
                        ?? throw new JsonException("Raw input snapshot is null.");
                }
                catch (Exception exception) when (exception is JsonException or NotSupportedException)
                {
                    var errorJson = JsonSerializer.Serialize(new[] { "INVALID_INPUT_SNAPSHOT" });
                    await _jobAnalysisRepository.MarkFailedAsync(runId, "INVALID_INPUT_SNAPSHOT", errorJson, ct);
                    return;
                }

                // A persisted marker means the previous worker may have exited
                // while the provider call was in flight. Do not spend another
                // request after restart; complete the run with the immutable raw
                // JD snapshot instead.
                if (run.ProviderCallStartedAt.HasValue)
                {
                    await CompleteRawFallbackAsync(run, "PROVIDER_CALL_INTERRUPTED", ct);
                    return;
                }

                if (!await _jobAnalysisRepository.TryMarkProviderCallStartedAsync(runId, ct))
                {
                    _logger.LogInformation("JobAnalysisProcessor: Run {RunId} was claimed by another worker before provider start.", runId);
                    return;
                }

                JobAnalysisExtractionResult extraction;
                var previousUserId = ITHunterview.Service.Utils.UserContext.CurrentUserId;
                ITHunterview.Service.Utils.UserContext.CurrentUserId = run.RequestedBy;

                try
                {
                    extraction = await _extractionService.ExtractAsync(
                        inputSnapshot,
                        systemPromptSnapshot.Content,
                        userPromptSnapshot.Content,
                        ct);
                }
                finally
                {
                    ITHunterview.Service.Utils.UserContext.CurrentUserId = previousUserId;
                }
                var validation = extraction.Validation;
                var quality = extraction.Quality != JdAnalysisQuality.INVALID
                    ? extraction.Quality
                    : validation.Quality;
                if (quality == JdAnalysisQuality.INVALID || validation.Data == null)
                {
                    await CompleteRawFallbackAsync(
                        run,
                        validation.FailureCode ?? "INVALID_MODEL_OUTPUT",
                        ct,
                        extraction.Diagnostics);
                    return;
                }

                var validatedData = validation.Data;
                var completionDiagnostics = extraction.Diagnostics
                    .GroupBy(diagnostic => $"{diagnostic.Code}:{diagnostic.JsonPath}", StringComparer.Ordinal)
                    .Select(group => group.First())
                    .Take(100)
                    .ToList();
                IReadOnlyList<SkillResolution> resolutions;
                try
                {
                    resolutions = await _skillResolver.ResolveAsync(validatedData.SkillsNormalized, ct);
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    _logger.LogWarning(
                        exception,
                        "JobAnalysisProcessor: Skill resolution unavailable for run {RunId}; preserving structured JD analysis.",
                        runId);
                    var diagnostic = new JdAnalysisDiagnostic("SKILL_RESOLUTION_UNAVAILABLE", "$.matching_metrics.skills_normalized");
                    if (completionDiagnostics.Count < 100 &&
                        !completionDiagnostics.Any(item => item.Code == diagnostic.Code && item.JsonPath == diagnostic.JsonPath))
                    {
                        completionDiagnostics.Add(diagnostic);
                    }
                    if (validatedData.Diagnostics.Count < 100 &&
                        !validatedData.Diagnostics.Any(item => item.Code == diagnostic.Code && item.JsonPath == diagnostic.JsonPath))
                    {
                        validatedData.Diagnostics.Add(diagnostic);
                    }
                    resolutions = Array.Empty<SkillResolution>();
                }

                var decisions = new List<JobSkillDecisions>();
                DateTime now = DateTime.UtcNow;

                foreach (var res in resolutions)
                {
                    // Recruiters are not asked to make technical decisions. A skill
                    // resolved to the active master dictionary becomes a tag/filter;
                    // an unresolved/ambiguous mention remains in requirements_list
                    // for detailed matching but is excluded from standardized tags.
                    var decisionStatus = res.ResolvedSkillId.HasValue
                        ? SkillDecisionStatus.ACCEPTED
                        : SkillDecisionStatus.REJECTED;

                    decisions.Add(new JobSkillDecisions
                    {
                        Id = Guid.NewGuid(),
                        JobAnalysisRunId = runId,
                        RawMention = res.RawMention,
                        NormalizedMention = res.NormalizedMention,
                        Category = res.Category,
                        Importance = res.Importance,
                        SourceSection = res.SourceSection,
                        EvidenceText = res.EvidenceText,
                        SuggestedSkillId = res.SuggestedSkillId,
                        ResolvedSkillId = res.ResolvedSkillId,
                        ResolutionStatus = res.ResolutionStatus,
                        DecisionStatus = decisionStatus,
                        DecisionVersion = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                string effectiveJson = _extractionService.SerializeEffectiveAnalysis(validatedData);

                bool completed = await _jobAnalysisRepository.TryCompleteReadyAsync(
                    runId,
                    new JobAnalysisCompletion(
                        run.InputRevision,
                        quality,
                        extraction.PersistableAnalysisJson ?? extraction.RawJson,
                        effectiveJson,
                        JdAnalysisMetadataReader.SerializeCoverage(extraction.Coverage),
                        JdAnalysisMetadataReader.SerializeDiagnostics(completionDiagnostics),
                        decisions,
                        extraction.ProviderName,
                        null),
                    ct);

                if (!completed)
                {
                    ITHunterview.Service.Utils.UserContext.CurrentUserId = null;
                }
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JobAnalysisProcessor: Unexpected error processing run {RunId}.", runId);
                string errJson = JsonSerializer.Serialize(new[] { ex.Message });
                await _jobAnalysisRepository.MarkFailedAsync(runId, "INTERNAL_ANALYSIS_ERROR", errJson, ct);
            }
        }

        private async Task CompleteRawFallbackAsync(
            JobAnalysisRuns run,
            string failureCode,
            CancellationToken ct,
            IReadOnlyList<JdAnalysisDiagnostic>? diagnostics = null)
        {
            var boundedDiagnostics = diagnostics?.Take(100).ToList()
                ?? new List<JdAnalysisDiagnostic>
                {
                    new(failureCode, "$")
                };
            await _jobAnalysisRepository.TryCompleteReadyAsync(
                run.Id,
                new JobAnalysisCompletion(
                    run.InputRevision,
                    JdAnalysisQuality.INVALID,
                    null,
                    null,
                    JdAnalysisMetadataReader.SerializeCoverage(new JdAnalysisCoverage(0, 0, 0, 0, 0, 0, false)),
                    JdAnalysisMetadataReader.SerializeDiagnostics(boundedDiagnostics),
                    Array.Empty<JobSkillDecisions>(),
                    run.ProviderName,
                    run.ModelName),
                ct);
        }

    }
}
