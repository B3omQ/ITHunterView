using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Utils;
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
                    inputSnapshot = JsonSerializer.Deserialize<JobAnalysisInputSnapshot>(run.RawInputSnapshot) ?? new JobAnalysisInputSnapshot();
                }
                catch
                {
                    inputSnapshot = new JobAnalysisInputSnapshot();
                }

                var extraction = await _extractionService.ExtractAsync(inputSnapshot, systemPromptSnapshot.Content, userPromptSnapshot.Content, ct);
                var validation = extraction.Validation;
                if (!validation.IsValid || validation.Data == null)
                {
                    string failureCode = validation.FailureCode ?? "INVALID_MODEL_OUTPUT";
                    string errJson = JsonSerializer.Serialize(validation.Errors);
                    _logger.LogWarning("JobAnalysisProcessor: Validation failed for run {RunId} with code '{FailureCode}'. Errors: {ErrJson}", runId, failureCode, errJson);
                    await _jobAnalysisRepository.MarkFailedAsync(runId, failureCode, errJson, ct);
                    return;
                }

                var validatedData = validation.Data;
                var resolutions = await _skillResolver.ResolveAsync(validatedData.SkillsNormalized, ct);

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
                        Confidence = res.Confidence,
                        DecisionVersion = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                string effectiveJson = _extractionService.SerializeEffectiveAnalysis(
                    validatedData,
                    resolutions
                        .Where(resolution => resolution.ResolvedSkillId.HasValue)
                        .Select(resolution => resolution.NormalizedMention)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase));

                bool completed = await _jobAnalysisRepository.TryCompleteReadyAsync(
                    runId,
                    run.InputRevision,
                    extraction.RawJson,
                    effectiveJson,
                    decisions,
                    extraction.ProviderName,
                    null,
                    ct);

                if (!completed)
                {
                    _logger.LogInformation("JobAnalysisProcessor: Run {RunId} was superseded or revision mismatched during completion.", runId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "JobAnalysisProcessor: Unexpected error processing run {RunId}.", runId);
                string errJson = JsonSerializer.Serialize(new[] { ex.Message });
                await _jobAnalysisRepository.MarkFailedAsync(runId, "INTERNAL_ANALYSIS_ERROR", errJson, ct);
            }
        }

    }
}
