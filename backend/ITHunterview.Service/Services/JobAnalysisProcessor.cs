using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Helpers;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Validators;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Services
{
    public interface IJobAnalysisProcessor
    {
        Task ProcessAsync(Guid runId, CancellationToken ct = default);
    }

    public class JobAnalysisProcessor : IJobAnalysisProcessor
    {
        private readonly IJobAnalysisRepository _jobAnalysisRepository;
        private readonly IPromptManagementService _promptService;
        private readonly IAiService _aiService;
        private readonly IJdAnalysisResponseValidator _validator;
        private readonly ISkillResolver _skillResolver;
        private readonly ILogger<JobAnalysisProcessor> _logger;

        public JobAnalysisProcessor(
            IJobAnalysisRepository jobAnalysisRepository,
            IPromptManagementService promptService,
            IAiService aiService,
            IJdAnalysisResponseValidator validator,
            ISkillResolver skillResolver,
            ILogger<JobAnalysisProcessor> logger)
        {
            _jobAnalysisRepository = jobAnalysisRepository ?? throw new ArgumentNullException(nameof(jobAnalysisRepository));
            _promptService = promptService ?? throw new ArgumentNullException(nameof(promptService));
            _aiService = aiService ?? throw new ArgumentNullException(nameof(aiService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _skillResolver = skillResolver ?? throw new ArgumentNullException(nameof(skillResolver));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task ProcessAsync(Guid runId, CancellationToken ct = default)
        {
            var run = await _jobAnalysisRepository.GetRunAsync(runId, ct);
            if (run == null || run.Status != JobAnalysisStatus.PROCESSING)
            {
                _logger.LogWarning($"JobAnalysisProcessor: Run {runId} is null or not in PROCESSING state.");
                return;
            }

            try
            {
                var systemPromptSnapshot = await _promptService.GetPromptSnapshotByVersionIdAsync(run.SystemPromptVersionId, ct);
                var userPromptSnapshot = await _promptService.GetPromptSnapshotByVersionIdAsync(run.UserPromptVersionId, ct);

                string systemPrompt = systemPromptSnapshot.Content;
                string userPrompt = userPromptSnapshot.Content.Replace("[JOB_INPUT_JSON]", run.RawInputSnapshot);

                string providerName = await _aiService.GetActiveProviderNameAsync();
                string aiResponse = await _aiService.GenerateTextAsync(userPrompt, systemPrompt, providerName);

                JobAnalysisInputSnapshot inputSnapshot;
                try
                {
                    inputSnapshot = JsonSerializer.Deserialize<JobAnalysisInputSnapshot>(run.RawInputSnapshot) ?? new JobAnalysisInputSnapshot();
                }
                catch
                {
                    inputSnapshot = new JobAnalysisInputSnapshot();
                }

                var validation = _validator.Validate(aiResponse, inputSnapshot);
                if (!validation.IsValid || validation.Data == null)
                {
                    string failureCode = validation.FailureCode ?? "INVALID_MODEL_OUTPUT";
                    string errJson = JsonSerializer.Serialize(validation.Errors);
                    _logger.LogWarning($"JobAnalysisProcessor: Validation failed for run {runId} with code '{failureCode}'. Errors: {errJson}");
                    await _jobAnalysisRepository.MarkFailedAsync(runId, failureCode, errJson, ct);
                    return;
                }

                var validatedData = validation.Data;
                var resolutions = await _skillResolver.ResolveAsync(validatedData.SkillsNormalized, ct);

                var decisions = new List<JobSkillDecisions>();
                DateTime now = DateTime.UtcNow;

                foreach (var res in resolutions)
                {
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
                        DecisionStatus = SkillDecisionStatus.PENDING,
                        Confidence = res.Confidence,
                        DecisionVersion = 1,
                        CreatedAt = now,
                        UpdatedAt = now
                    });
                }

                string effectiveJson = JsonSerializer.Serialize(new
                {
                    schema_version = validatedData.SchemaVersion,
                    matching_metrics = new
                    {
                        job_titles_normalized = validatedData.JobTitlesNormalized,
                        skills_normalized = validatedData.SkillsNormalized.Select(s => new
                        {
                            name = s.Name,
                            category = s.Category,
                            raw_mention = s.RawMention,
                            source_section = s.SourceSection,
                            evidence = s.Evidence,
                            confidence = s.Confidence
                        }),
                        total_years_exp = validatedData.TotalYearsExp,
                        domains = validatedData.Domains,
                        requirements_list = validatedData.RequirementsList.Select(r => new
                        {
                            category = r.Category,
                            importance = r.Importance,
                            skill_name = r.SkillName,
                            detail_verbatim = r.DetailVerbatim,
                            raw_mention = r.RawMention,
                            source_section = r.SourceSection,
                            evidence = r.Evidence,
                            confidence = r.Confidence
                        })
                    }
                });

                bool completed = await _jobAnalysisRepository.TryCompleteReadyAsync(
                    runId,
                    run.InputRevision,
                    aiResponse,
                    effectiveJson,
                    decisions,
                    providerName,
                    "gemini-flash",
                    ct);

                if (!completed)
                {
                    _logger.LogInformation($"JobAnalysisProcessor: Run {runId} was superseded or revision mismatched during completion.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"JobAnalysisProcessor: Unexpected error processing run {runId}.");
                string errJson = JsonSerializer.Serialize(new[] { ex.Message });
                await _jobAnalysisRepository.MarkFailedAsync(runId, "INTERNAL_ANALYSIS_ERROR", errJson, ct);
            }
        }
    }
}
