using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Service.Matching;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.UseCase
{
    public class LearningPathUseCase : ILearningPathUseCase
    {
        private readonly ILearningPathRepository _learningPathRepository;
        private readonly IInterviewAnswerRepository _interviewAnswerRepository;
        private readonly IInterviewSessionRepository _interviewSessionRepository;
        private readonly IAiService _aiService;
        private readonly ITHunterviewContext _context;
        private readonly IJdMatchReportReader _matchReportReader;

        public LearningPathUseCase(
            ILearningPathRepository learningPathRepository,
            IInterviewAnswerRepository interviewAnswerRepository,
            IInterviewSessionRepository interviewSessionRepository,
            IAiService aiService,
            ITHunterviewContext context,
            IJdMatchReportReader? matchReportReader = null)
        {
            _learningPathRepository = learningPathRepository;
            _interviewAnswerRepository = interviewAnswerRepository;
            _interviewSessionRepository = interviewSessionRepository;
            _aiService = aiService;
            _context = context;
            _matchReportReader = matchReportReader ?? new JdMatchReportReader();
        }


        // ─────────────────────────────────────────────────────────────
        // Target Roles
        // ─────────────────────────────────────────────────────────────
        public async Task<List<TargetRoleResponseDto>> GetTargetRolesAsync()
        {
            var templates = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .ThenInclude(rs => rs.SfiaSkill)
                .ToListAsync();

            return templates.Select(t => new TargetRoleResponseDto
            {
                Id = t.Id,
                RoleName = t.RoleName,
                Description = t.Description,
                RequiredSkills = t.RequiredSkills.Select(rs => new TargetRoleSkillDto
                {
                    SkillCode = rs.SfiaSkill.SkillCode,
                    SkillName = rs.SfiaSkill.SkillName,
                    Description = rs.SfiaSkill.Description ?? "",
                    AvailableLevels = rs.SfiaSkill.AvailableLevels ?? "",
                    TargetLevel = rs.TargetLevel
                }).ToList()
            }).ToList();
        }

        // ─────────────────────────────────────────────────────────────
        // Generate từ input thủ công (giữ nguyên)
        // ─────────────────────────────────────────────────────────────
        public async Task<LearningPathResponseDto> GenerateLearningPathAsync(Guid candidateId, GeneratePathRequestDto request)
        {
            await EnforceMaxPathsAsync(candidateId);

            string roleName = string.Empty;
            var gaps = new List<object>();

            if (request.TargetRoleTemplateId != null && request.TargetRoleTemplateId != Guid.Empty)
            {
                var template = await _context.TargetRoleTemplates
                    .Include(t => t.RequiredSkills)
                    .ThenInclude(rs => rs.SfiaSkill)
                    .FirstOrDefaultAsync(t => t.Id == request.TargetRoleTemplateId);

                if (template == null)
                    throw new ArgumentException("Target role template not found.");

                roleName = template.RoleName;
                var userSkillsDict = request.CurrentSkills.ToDictionary(s => s.SkillCode, s => s.CurrentLevel);

                foreach (var requiredSkill in template.RequiredSkills)
                {
                    int currentLevel = userSkillsDict.ContainsKey(requiredSkill.SfiaSkill.SkillCode) ? userSkillsDict[requiredSkill.SfiaSkill.SkillCode] : 0;
                    int gap = requiredSkill.TargetLevel - currentLevel;

                    if (gap > 0)
                    {
                        gaps.Add(new
                        {
                            skill_code = requiredSkill.SfiaSkill.SkillCode,
                            skill_name = requiredSkill.SfiaSkill.SkillName,
                            current_level = currentLevel,
                            target_level = requiredSkill.TargetLevel,
                            gap_delta = gap
                        });
                    }
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(request.CustomTargetRoleName) || request.CustomTargetSkills == null || !request.CustomTargetSkills.Any())
                {
                    throw new ArgumentException("Custom target role name and skills are required when TargetRoleTemplateId is not provided.");
                }

                roleName = request.CustomTargetRoleName;
                var allSkills = await _context.SfiaSkills.ToDictionaryAsync(s => s.SkillCode, s => s.SkillName);

                foreach (var customSkill in request.CustomTargetSkills)
                {
                    int gap = customSkill.TargetLevel - customSkill.CurrentLevel;
                    if (gap > 0)
                    {
                        string skillName = allSkills.ContainsKey(customSkill.SkillCode) ? allSkills[customSkill.SkillCode] : customSkill.SkillCode;
                        gaps.Add(new
                        {
                            skill_code = customSkill.SkillCode,
                            skill_name = skillName,
                            current_level = customSkill.CurrentLevel,
                            target_level = customSkill.TargetLevel,
                            gap_delta = gap
                        });
                    }
                }
            }

            string systemPrompt = @"You are an expert IT career coach. 
Generate a comprehensive, step-by-step learning path based on the user's SFIA skill gaps.
The result MUST be a valid JSON object strictly following this schema:
{
  ""title"": ""Path to Senior Backend Developer"",
  ""target_profile"": { ""role_name"": ""..."", ""description"": ""..."" },
  ""gap_summary"": {
    ""total_gaps"": 2,
    ""gaps"": [ { ""skill_code"": ""PROG"", ""skill_name"": ""..."", ""current_level"": 3, ""target_level"": 5, ""gap_delta"": 2 } ]
  },
  ""modules"": [
    {
      ""module_index"": 0,
      ""title"": ""Module 1: PROG Level 3 to 4"",
      ""description"": ""..."",
      ""sfia_target"": { ""skill_code"": ""PROG"", ""from_level"": 3, ""to_level"": 4 },
      ""tasks"": [
        { ""task_index"": 0, ""title"": ""..."", ""description"": ""..."", ""estimated_hours"": 8 }
      ]
    }
  ],
  ""progress"": { ""total_modules"": 1, ""completed_modules"": 0, ""total_tasks"": 1, ""completed_tasks"": 0, ""percentage"": 0 }
}
Rule: Create one module per 1 level jump per skill. If gap is 2 levels, create 2 sequential modules for that skill.
Do NOT include any markdown blocks like ```json, just return the raw JSON object.";

            var userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine($"Target Role: {roleName}");
            userPromptBuilder.AppendLine();
            
            userPromptBuilder.AppendLine("=== SFIA SKILL GAPS ===");
            userPromptBuilder.AppendLine(JsonSerializer.Serialize(gaps));
            userPromptBuilder.AppendLine();

            if (!string.IsNullOrWhiteSpace(request.PersonalContext))
            {
                userPromptBuilder.AppendLine("=== CANDIDATE'S PERSONAL CONTEXT & PRIOR KNOWLEDGE ===");
                userPromptBuilder.AppendLine(request.PersonalContext);
                userPromptBuilder.AppendLine("Rule: Use this context to skip basic topics the candidate already knows, even if their current formal SFIA level is low. Tailor the learning tasks specifically to their actual starting point and context.");
                userPromptBuilder.AppendLine();
            }
            
            userPromptBuilder.AppendLine("Please generate a structured, highly personalized self-paced learning path following the SFIA progression rules.");

            string userPrompt = userPromptBuilder.ToString();

            return await CallAiAndSaveAsync(candidateId, userPrompt, systemPrompt);
        }

        // ─────────────────────────────────────────────────────────────
        // Generate từ lịch sử matching CV-JD & phỏng vấn
        // ─────────────────────────────────────────────────────────────
        public async Task<ExtractSfiaProfileResponseDto> ExtractFromCvJdAsync(Guid candidateId, Guid matchScoreId)
        {
            var matchScore = await _context.CvJobMatchScores
                .FirstOrDefaultAsync(m => m.Id == matchScoreId && m.UserId == candidateId);
            if (matchScore != null && !string.IsNullOrWhiteSpace(matchScore.SfiaExtractResult))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<ExtractSfiaProfileResponseDto>(matchScore.SfiaExtractResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cached != null) return cached;
                }
                catch { /* Ignore and re-extract if parse fails */ }
            }

            var matchContext = await BuildMatchContextAsync(candidateId, matchScoreId);
            if (string.IsNullOrWhiteSpace(matchContext))
                throw new InvalidOperationException("Chưa có dữ liệu matching CV-JD.");

            var result = await PerformExtractionAsync(matchContext, "CV-JD Matching");

            if (matchScore != null)
            {
                matchScore.SfiaExtractResult = JsonSerializer.Serialize(result);
                await _context.SaveChangesAsync();
            }

            return result;
        }

        public async Task<ExtractSfiaProfileResponseDto> ExtractFromInterviewAsync(Guid candidateId, Guid sessionId)
        {
            var session = await _context.InterviewSessions.FirstOrDefaultAsync(s => s.Id == sessionId);
            if (session != null && !string.IsNullOrWhiteSpace(session.SfiaExtractResult))
            {
                try
                {
                    var cached = JsonSerializer.Deserialize<ExtractSfiaProfileResponseDto>(session.SfiaExtractResult, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (cached != null) return cached;
                }
                catch { /* Ignore and re-extract if parse fails */ }
            }

            var interviewContext = await BuildInterviewContextAsync(candidateId, sessionId);
            if (string.IsNullOrWhiteSpace(interviewContext))
                throw new InvalidOperationException("Chưa có dữ liệu phỏng vấn thử.");

            var result = await PerformExtractionAsync(interviewContext, "Mock Interview");

            if (session != null)
            {
                session.SfiaExtractResult = JsonSerializer.Serialize(result);
                await _context.SaveChangesAsync();
            }

            return result;
        }

        private async Task<ExtractSfiaProfileResponseDto> PerformExtractionAsync(string contextText, string sourceName)
        {
            var allSkills = await _context.SfiaSkills.ToListAsync();
            var allSkillsJson = JsonSerializer.Serialize(allSkills.Select(s => new { s.SkillCode, s.SkillName }));

            var genericLevelsJson = JsonSerializer.Serialize(ITHunterview.Service.Constant.SfiaGenericLevels.Matrix.Select(x => new { Level = x.Key, Description = x.Value.Essence }));

            string systemPrompt = @"You are an expert IT career coach and data extractor.
Analyze the candidate's skill gaps identified from their " + sourceName + @" context.
You will be provided a list of all 147 SFIA skills AND a guide on SFIA Generic Levels (1-7).
Your job is to:
1. Define a highly relevant `customRoleName` based on the candidate's target job and context.
2. Provide a short `customRoleDescription`.
3. Identify the EXACT SFIA skills the candidate needs to develop or demonstrate to bridge the gaps identified in the context. DO NOT snap to predefined templates. Tailor this specifically to the context.
4. For EACH identified skill, provide:
   - `skillCode`: the SFIA skill code.
   - `targetLevel`: the expected proficiency level for the role (1-7).
   - `currentLevel`: estimate the candidate's CURRENT proficiency level (0-7).
   - `justification`: brief reasoning for the gap and levels assigned.
The result MUST be a valid JSON object strictly following this schema:
{
  ""customRoleName"": ""..."",
  ""customRoleDescription"": ""..."",
  ""skills"": [
    { ""skillCode"": ""..."", ""targetLevel"": 4, ""currentLevel"": 2, ""justification"": ""..."" }
  ]
}
Do NOT include any markdown blocks like ```json, just return the raw JSON object.";

            var userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine("=== ALL SFIA SKILLS ===");
            userPromptBuilder.AppendLine(allSkillsJson);
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("=== SFIA GENERIC LEVELS GUIDE ===");
            userPromptBuilder.AppendLine(genericLevelsJson);
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("=== CANDIDATE CONTEXT ===");
            userPromptBuilder.AppendLine(contextText);

            var aiResponseText = await _aiService.GenerateTextAsync(userPromptBuilder.ToString(), systemPrompt);

            aiResponseText = aiResponseText.Trim();
            if (aiResponseText.StartsWith("```json")) aiResponseText = aiResponseText.Substring(7);
            if (aiResponseText.StartsWith("```")) aiResponseText = aiResponseText.Substring(3);
            if (aiResponseText.EndsWith("```")) aiResponseText = aiResponseText.Substring(0, aiResponseText.Length - 3);
            aiResponseText = aiResponseText.Trim();

            try
            {
                var dto = JsonSerializer.Deserialize<ExtractSfiaProfileResponseDto>(aiResponseText, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                       ?? throw new Exception("AI returned null object.");

                return dto;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Không thể trích xuất SFIA Profile từ AI: {ex.Message}");
            }
        }

        public async Task<HistoryContextPreviewDto> PreviewHistoryContextAsync(Guid candidateId, string type, Guid? sourceId)
        {
            string context = string.Empty;
            if (type == "cv-jd")
            {
                context = await BuildMatchContextAsync(candidateId, sourceId);
                if (string.IsNullOrWhiteSpace(context))
                {
                    context = "Không tìm thấy dữ liệu lỗ hổng kỹ năng cho bản ghi CV-JD Matching này.";
                }
            }
            else if (type == "interview")
            {
                context = await BuildInterviewContextAsync(candidateId, sourceId);
                if (string.IsNullOrWhiteSpace(context))
                {
                    context = "Không tìm thấy dữ liệu đánh giá cho buổi phỏng vấn thử này.";
                }
            }
            else
            {
                context = "Loại lịch sử không hợp lệ.";
            }

            return new HistoryContextPreviewDto { ContextPreview = context };
        }

        // ─────────────────────────────────────────────────────────────
        // Private helpers
        // ─────────────────────────────────────────────────────────────

        private async Task EnforceMaxPathsAsync(Guid candidateId)
        {
            var activeSub = await _context.UserSubscriptions
                .AsNoTracking()
                .Where(us => us.UserId == candidateId && us.Status == Domain.Enums.UserSubscriptionStatus.ACTIVE && us.EndDate >= DateTime.UtcNow)
                .OrderByDescending(us => us.EndDate)
                .FirstOrDefaultAsync();

            int slotLimit = 1; // Mặc định gói Basic là 1 slot
            if (activeSub != null)
            {
                var subscription = await _context.Subscriptions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.Id == activeSub.SubId && s.Status == Domain.Enums.SubscriptionStatus.ACTIVE);

                if (subscription != null && !string.IsNullOrEmpty(subscription.FeaturesConfig))
                {
                    try
                    {
                        var features = JsonSerializer.Deserialize<DTOs.Subscription.FeaturesConfigDto>(
                            subscription.FeaturesConfig,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                        if (features?.LearningPathSlotLimit != null)
                        {
                            slotLimit = features.LearningPathSlotLimit.Value;
                        }
                    }
                    catch
                    {
                        // Bỏ qua lỗi JSON, dùng limit mặc định
                    }
                }
            }

            if (slotLimit == -1 || slotLimit >= 999)
            {
                return; // Gói Mastery hoặc Unlimited cho phép lưu vô hạn lộ trình
            }

            var existingPaths = await _learningPathRepository.GetByCandidateIdAsync(candidateId);
            if (existingPaths.Count >= slotLimit)
            {
                throw new InvalidOperationException(
                    $"Bạn đã đạt giới hạn tối đa {slotLimit} lộ trình học hoạt động trên bảng điều khiển của gói hiện tại. " +
                    "Vui lòng xoá bớt một lộ trình cũ hoặc nâng cấp gói (như Mastery với slot vô hạn) trước khi tạo lộ trình mới.");
            }
        }

        private async Task<string> BuildMatchContextAsync(Guid candidateId, Guid? matchScoreId)
        {
            CvJobMatchScores matchRecord;

            if (matchScoreId.HasValue)
            {
                matchRecord = await _context.CvJobMatchScores
                    .FirstOrDefaultAsync(m => m.Id == matchScoreId.Value && m.UserId == candidateId);
            }
            else
            {
                // Lấy bản ghi Completed mới nhất của candidate
                matchRecord = await _context.CvJobMatchScores
                    .Where(m => m.UserId == candidateId && m.Status == "Completed")
                    .OrderByDescending(m => m.UpdatedAt)
                    .FirstOrDefaultAsync();
            }

            if (matchRecord == null || string.IsNullOrWhiteSpace(matchRecord.MatchDetails))
                return string.Empty;

            var sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(matchRecord.JdTitle))
                sb.AppendLine($"Target Job: {matchRecord.JdTitle}");

            var report = _matchReportReader.Read(
                matchRecord.MatchDetails,
                matchRecord.MatchScore,
                matchRecord.MatchType);
            sb.AppendLine($"Overall Match Score: {report.ScorePercent.ToString("F1", CultureInfo.InvariantCulture)}/100");

            if (!string.IsNullOrWhiteSpace(report.Narrative))
                sb.AppendLine($"AI Narrative Assessment: {report.Narrative}");

            if (report.ReportKind == MatchReportKinds.Structured)
            {
                AppendRequirementContext(sb, report);
                AppendCriticalGapContext(sb, report);
            }
            else if (report.MatchMethod == MatchMethodCodes.Hardcode)
            {
                sb.AppendLine("Matching Method: Keyword-based (Hardcode)");
                sb.AppendLine("Note: Keyword-based matching does not provide specific requirement evidence. The AI will generate a general path based on the target role.");
            }
            else if (report.ReportKind == MatchReportKinds.RawTextFallback)
            {
                sb.AppendLine("Matching Method: AI evaluation from raw JD text.");
            }
            else
            {
                sb.AppendLine("Matching details are unavailable for this legacy result.");
            }

            return sb.ToString();
        }

        private static void AppendRequirementContext(StringBuilder sb, MatchReportDto report)
        {
            var items = report.RequirementGroups
                .SelectMany(group => SelectItemsForLearningPath(group)
                    .Select(item => (Group: group, Item: item)))
                .ToList();

            AppendRequirementSection(
                sb,
                "Identified Skill Gaps & Weaknesses:",
                items.Where(entry => entry.Item.Score < 0.8m));
            AppendRequirementSection(
                sb,
                "Identified Strengths & Mastered Skills:",
                items.Where(entry => entry.Item.Score >= 0.8m));
        }

        private static IEnumerable<MatchRequirementItemReportDto> SelectItemsForLearningPath(
            MatchRequirementGroupReportDto group)
        {
            if (group.Operator is not ("one_of" or "at_least_n"))
            {
                return group.Items;
            }

            if (group.SelectedItemIds.Count == 0)
            {
                return Array.Empty<MatchRequirementItemReportDto>();
            }

            var selected = group.SelectedItemIds.ToHashSet(StringComparer.Ordinal);
            return group.Items.Where(item => item.ItemId != null && selected.Contains(item.ItemId));
        }

        private static void AppendRequirementSection(
            StringBuilder sb,
            string heading,
            IEnumerable<(MatchRequirementGroupReportDto Group, MatchRequirementItemReportDto Item)> entries)
        {
            var materialized = entries.ToList();
            if (materialized.Count == 0) return;

            sb.AppendLine(heading);
            foreach (var (group, item) in materialized)
            {
                var requirement = item.NormalizedText
                    ?? item.DetailVerbatim
                    ?? item.RawMention
                    ?? group.RequirementVerbatim
                    ?? "Unknown requirement";
                var score = item.Score.ToString("0.###", CultureInfo.InvariantCulture);
                sb.AppendLine($"- {requirement} (Score: {score}): {item.Reasoning}");
                foreach (var evidence in item.Evidence)
                {
                    var section = string.IsNullOrWhiteSpace(evidence.Section) ? string.Empty : $" [{evidence.Section}]";
                    sb.AppendLine($"  Evidence{section}: {evidence.Quotation}");
                }
            }
        }

        private static void AppendCriticalGapContext(StringBuilder sb, MatchReportDto report)
        {
            var gaps = report.CriticalGaps
                .Select(gap => DescribeCriticalGap(report, gap))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            if (gaps.Count > 0)
                sb.AppendLine($"Critical Gaps: {string.Join("; ", gaps)}");
        }

        private static string DescribeCriticalGap(
            MatchReportDto report,
            MatchCriticalGapReportDto gap)
        {
            var requirement = gap.Requirement;
            if (string.IsNullOrWhiteSpace(requirement) && gap.AffectedItemIds.Count > 0)
            {
                var affected = gap.AffectedItemIds.ToHashSet(StringComparer.Ordinal);
                var labels = report.RequirementGroups
                    .Where(group => string.Equals(group.GroupId, gap.GroupId, StringComparison.Ordinal))
                    .SelectMany(group => group.Items)
                    .Where(item => item.ItemId != null && affected.Contains(item.ItemId))
                    .Select(item => item.NormalizedText ?? item.DetailVerbatim ?? item.RawMention)
                    .Where(label => !string.IsNullOrWhiteSpace(label))
                    .Cast<string>()
                    .ToList();
                requirement = gap.Operator switch
                {
                    "one_of" => string.Join(" | ", labels),
                    "at_least_n" => $"{gap.SatisfiedCount ?? 0}/{gap.RequiredCount ?? 0}: {string.Join(", ", labels)}",
                    _ => string.Join(", ", labels)
                };
            }

            if (string.IsNullOrWhiteSpace(requirement)) return string.Empty;
            return string.IsNullOrWhiteSpace(gap.Reasoning)
                ? requirement
                : $"{requirement} - {gap.Reasoning}";
        }

        private async Task<string> BuildInterviewContextAsync(Guid candidateId, Guid? sessionId)
        {
            // Lấy session
            ITHunterview.Domain.Entities.InterviewSessions session;

            if (sessionId.HasValue)
            {
                session = await _context.InterviewSessions
                    .FirstOrDefaultAsync(s => s.Id == sessionId.Value && s.CandidateId == candidateId);
            }
            else
            {
                session = await _context.InterviewSessions
                    .Where(s => s.CandidateId == candidateId &&
                                s.Status == ITHunterview.Domain.Enums.InterviewSessionStatus.COMPLETED)
                    .OrderByDescending(s => s.EndedAt)
                    .FirstOrDefaultAsync();
            }

            if (session == null)
                return string.Empty;

            var answers = await _interviewAnswerRepository.GetBySessionIdAsync(session.Id);
            var scoredAnswers = answers
                .Where(a => a.CandidateTranscript != null &&
                            (a.ScoreTech.HasValue || a.ScoreCommunication.HasValue))
                .ToList();

            if (!scoredAnswers.Any())
                return string.Empty;

            var sb = new StringBuilder();
            sb.AppendLine($"Interview Session: Difficulty = {session.DifficultyLevel}");

            // Tính điểm trung bình
            var avgTech = scoredAnswers
                .Where(a => a.ScoreTech.HasValue)
                .Average(a => (double)a.ScoreTech.Value);
            var avgComm = scoredAnswers
                .Where(a => a.ScoreCommunication.HasValue)
                .Average(a => (double)a.ScoreCommunication.Value);

            sb.AppendLine($"Average Technical Score: {avgTech:F1}/100");
            sb.AppendLine($"Average Communication Score: {avgComm:F1}/100");

            // Phân loại câu hỏi theo topic (dựa theo thứ tự câu hỏi — logic từ InterviewUseCase)
            var weakTopics = new List<string>();

            var skillsTurns = scoredAnswers.Take(2).ToList(); // Q1-Q2: Skills
            var expTurns = scoredAnswers.Skip(2).Take(2).ToList(); // Q3-Q4: Experience
            var jdTurns = scoredAnswers.Skip(4).Take(2).ToList(); // Q5-Q6: JD Match

            if (skillsTurns.Any())
            {
                var skillsAvgTech = skillsTurns.Where(a => a.ScoreTech.HasValue).Select(a => (double)a.ScoreTech.Value).DefaultIfEmpty(100).Average();
                if (skillsAvgTech < 60) weakTopics.Add($"Technical/Soft Skills (avg score: {skillsAvgTech:F0}/100)");
            }

            if (expTurns.Any())
            {
                var expAvgTech = expTurns.Where(a => a.ScoreTech.HasValue).Select(a => (double)a.ScoreTech.Value).DefaultIfEmpty(100).Average();
                if (expAvgTech < 60) weakTopics.Add($"Real-world Experience & Projects (avg score: {expAvgTech:F0}/100)");
            }

            if (jdTurns.Any())
            {
                var jdAvgTech = jdTurns.Where(a => a.ScoreTech.HasValue).Select(a => (double)a.ScoreTech.Value).DefaultIfEmpty(100).Average();
                if (jdAvgTech < 60) weakTopics.Add($"JD Fit & Situational Handling (avg score: {jdAvgTech:F0}/100)");
            }

            if (weakTopics.Any())
                sb.AppendLine($"Weak Areas (score < 60/100): {string.Join("; ", weakTopics)}");
            else
                sb.AppendLine("No critical weak areas detected (all topic scores >= 60/100). Focus on deepening existing strengths.");

            // Trích dẫn rubric chi tiết từ AiFeedback JSON nếu có
            var rubricInsights = new List<string>();
            foreach (var answer in scoredAnswers)
            {
                if (string.IsNullOrWhiteSpace(answer.AiFeedback)) continue;
                try
                {
                    using var rubricDoc = JsonDocument.Parse(answer.AiFeedback);
                    var rubric = rubricDoc.RootElement;

                    if (rubric.TryGetProperty("improvements", out var improvements) &&
                        improvements.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var imp in improvements.EnumerateArray())
                        {
                            var impStr = imp.GetString();
                            if (!string.IsNullOrWhiteSpace(impStr))
                                rubricInsights.Add(impStr);
                        }
                    }
                }
                catch { /* ignore non-JSON feedback */ }
            }

            if (rubricInsights.Any())
            {
                sb.AppendLine("Specific Improvement Areas from Interview Feedback:");
                foreach (var insight in rubricInsights.Distinct().Take(10))
                    sb.AppendLine($"  - {insight}");
            }

            return sb.ToString();
        }

        private async Task<LearningPathResponseDto> CallAiAndSaveAsync(Guid candidateId, string userPrompt, string systemPrompt)
        {
            var aiResponseText = await _aiService.GenerateTextAsync(userPrompt, systemPrompt);

            // Clean up markdown wrappers if AI adds them
            aiResponseText = aiResponseText.Trim();
            if (aiResponseText.StartsWith("```json"))
                aiResponseText = aiResponseText.Substring(7);
            if (aiResponseText.StartsWith("```"))
                aiResponseText = aiResponseText.Substring(3);
            if (aiResponseText.EndsWith("```"))
                aiResponseText = aiResponseText.Substring(0, aiResponseText.Length - 3);
            aiResponseText = aiResponseText.Trim();

            string parsedTitle = "Generated Learning Path";
            string serializedData = "{}";
            
            try
            {
                var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(aiResponseText) ?? new Dictionary<string, object>();
                
                if (rootDict.TryGetValue("title", out var titleObj) && titleObj is JsonElement titleElem && titleElem.ValueKind == JsonValueKind.String)
                {
                    parsedTitle = titleElem.GetString() ?? parsedTitle;
                }
                
                if (rootDict.TryGetValue("modules", out var modulesObj) && modulesObj is JsonElement modulesElem && modulesElem.ValueKind == JsonValueKind.Array)
                {
                    var modulesList = new List<Dictionary<string, object>>();
                    foreach (var mod in modulesElem.EnumerateArray())
                    {
                        var modDict = JsonSerializer.Deserialize<Dictionary<string, object>>(mod.GetRawText()) ?? new Dictionary<string, object>();
                        modDict["completed"] = false;
                        
                        if (modDict.TryGetValue("tasks", out var tasksObj) && tasksObj is JsonElement tasksElem && tasksElem.ValueKind == JsonValueKind.Array)
                        {
                            var tasksList = new List<Dictionary<string, object>>();
                            foreach (var task in tasksElem.EnumerateArray())
                            {
                                var taskDict = JsonSerializer.Deserialize<Dictionary<string, object>>(task.GetRawText()) ?? new Dictionary<string, object>();
                                taskDict["completed"] = false;
                                tasksList.Add(taskDict);
                            }
                            modDict["tasks"] = tasksList;
                        }
                        
                        modulesList.Add(modDict);
                    }
                    rootDict["modules"] = modulesList;
                }
                serializedData = JsonSerializer.Serialize(rootDict);
            }
            catch (Exception)
            {
                throw new Exception("AI generated an invalid JSON response. Please try again.");
            }

            var learningPath = new LearningPaths
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                Title = parsedTitle.Length > 255 ? parsedTitle.Substring(0, 255) : parsedTitle,
                Status = "Not Started",
                PathData = serializedData,
                CreatedAt = DateTime.UtcNow
            };

            await _learningPathRepository.AddAsync(learningPath);

            return new LearningPathResponseDto
            {
                Id = learningPath.Id,
                CandidateId = learningPath.CandidateId,
                Title = learningPath.Title,
                Status = learningPath.Status,
                PathData = JsonDocument.Parse(learningPath.PathData),
                CreatedAt = learningPath.CreatedAt
            };
        }

        // ─────────────────────────────────────────────────────────────
        // Query methods
        // ─────────────────────────────────────────────────────────────

        public async Task<List<LearningPathResponseDto>> GetMyLearningPathsAsync(Guid candidateId)
        {
            var paths = await _learningPathRepository.GetByCandidateIdAsync(candidateId);
            return paths.Select(p => new LearningPathResponseDto
            {
                Id = p.Id,
                CandidateId = p.CandidateId,
                Title = p.Title,
                Status = p.Status,
                PathData = JsonDocument.Parse(p.PathData),
                CreatedAt = p.CreatedAt
            }).ToList();
        }

        public async Task<LearningPathResponseDto> GetLearningPathByIdAsync(Guid candidateId, Guid id)
        {
            var path = await _learningPathRepository.GetByIdAsync(id);
            if (path == null || path.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Learning path not found.");
            }

            return new LearningPathResponseDto
            {
                Id = path.Id,
                CandidateId = path.CandidateId,
                Title = path.Title,
                Status = path.Status,
                PathData = JsonDocument.Parse(path.PathData),
                CreatedAt = path.CreatedAt
            };
        }

        public async Task DeleteLearningPathAsync(Guid candidateId, Guid id)
        {
            var path = await _learningPathRepository.GetByIdAsync(id);
            if (path == null || path.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Learning path not found or access denied.");
            }

            await _learningPathRepository.DeleteAsync(path);
        }

        public async Task<LearningPathResponseDto> ToggleTaskCompletionAsync(Guid candidateId, Guid pathId, int moduleIndex, int taskIndex)
        {
            var path = await _learningPathRepository.GetByIdAsync(pathId);
            if (path == null || path.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Learning path not found or access denied.");
            }

            var rootDict = JsonSerializer.Deserialize<Dictionary<string, object>>(path.PathData);
            if (rootDict == null || !rootDict.TryGetValue("modules", out var modulesObj) || !(modulesObj is JsonElement modulesElem) || modulesElem.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("Invalid path data format. Modules not found.");
            }

            var modules = new List<Dictionary<string, object>>();
            foreach (var m in modulesElem.EnumerateArray())
            {
                modules.Add(JsonSerializer.Deserialize<Dictionary<string, object>>(m.GetRawText()) ?? new Dictionary<string, object>());
            }

            if (moduleIndex < 0 || moduleIndex >= modules.Count)
            {
                throw new ArgumentException("Invalid module index.");
            }

            if (!modules[moduleIndex].TryGetValue("tasks", out var tasksObj) || !(tasksObj is JsonElement tasksElem) || tasksElem.ValueKind != JsonValueKind.Array)
            {
                throw new ArgumentException("This module does not contain any tasks.");
            }

            var tasksList = new List<Dictionary<string, object>>();
            foreach (var t in tasksElem.EnumerateArray())
            {
                tasksList.Add(JsonSerializer.Deserialize<Dictionary<string, object>>(t.GetRawText()) ?? new Dictionary<string, object>());
            }

            if (taskIndex < 0 || taskIndex >= tasksList.Count)
            {
                throw new ArgumentException("Invalid task index.");
            }

            // Determine current status
            bool currentTaskStatus = tasksList[taskIndex].TryGetValue("completed", out var compVal) && IsTrue(compVal);

            // Determine intended next status
            bool nextStatus = !currentTaskStatus;

            // Enforcement Rules
            if (nextStatus) // Checking
            {
                // Check previous module is completed
                if (moduleIndex > 0)
                {
                    if (!IsModuleCompleted(modules[moduleIndex - 1]))
                        throw new ArgumentException("You must complete the previous module first.");
                }

                // Check previous task in current module is completed
                if (taskIndex > 0)
                {
                    if (!tasksList[taskIndex - 1].TryGetValue("completed", out var prevTaskVal) || !IsTrue(prevTaskVal))
                        throw new ArgumentException("You must complete the previous task first.");
                }
            }
            else // Unchecking
            {
                // Ensure next task in current module is not completed
                if (taskIndex < tasksList.Count - 1)
                {
                    if (tasksList[taskIndex + 1].TryGetValue("completed", out var nextTaskVal) && IsTrue(nextTaskVal))
                        throw new ArgumentException("Cannot uncheck task because the subsequent task is already completed.");
                }
                // Ensure first task of next module is not completed
                else if (moduleIndex < modules.Count - 1)
                {
                    if (modules[moduleIndex + 1].TryGetValue("tasks", out var nextModTasksObj) && nextModTasksObj is JsonElement nextModTasksElem && nextModTasksElem.ValueKind == JsonValueKind.Array)
                    {
                        var nextModTasksList = nextModTasksElem.EnumerateArray().ToList();
                        if (nextModTasksList.Count > 0)
                        {
                            if (nextModTasksList[0].TryGetProperty("completed", out var firstTaskNextMod) && firstTaskNextMod.ValueKind == JsonValueKind.True)
                                throw new ArgumentException("Cannot uncheck task because the next module has already been started.");
                        }
                    }
                }
            }

            tasksList[taskIndex]["completed"] = nextStatus;
            modules[moduleIndex]["tasks"] = tasksList;

            // Update module completion status based on its tasks (handling both bool and JsonElement)
            bool isModuleCompleted = tasksList.All(t => t.TryGetValue("completed", out var cv) && IsTrue(cv));
            modules[moduleIndex]["completed"] = isModuleCompleted;

            // Auto-heal completed status for all modules in case of previously saved inconsistencies
            for (int i = 0; i < modules.Count; i++)
            {
                if (i == moduleIndex) continue;
                if (modules[i].TryGetValue("tasks", out var tObj) && tObj is JsonElement tElem && tElem.ValueKind == JsonValueKind.Array)
                {
                    var tList = tElem.EnumerateArray().ToList();
                    if (tList.Count > 0 && tList.All(t => t.TryGetProperty("completed", out var cv) && cv.ValueKind == JsonValueKind.True))
                    {
                        modules[i]["completed"] = true;
                    }
                }
            }

            rootDict["modules"] = modules;
            path.PathData = JsonSerializer.Serialize(rootDict);

            // Recalculate global status based on ALL tasks across ALL modules
            var updatedDoc = JsonDocument.Parse(path.PathData);
            
            int totalTasks = 0;
            int totalCompletedTasks = 0;
            int completedModulesCount = 0;

            if (updatedDoc.RootElement.TryGetProperty("modules", out var upModulesElem) && upModulesElem.ValueKind == JsonValueKind.Array)
            {
                foreach (var mod in upModulesElem.EnumerateArray())
                {
                    if (mod.TryGetProperty("completed", out var modComp) && modComp.ValueKind == JsonValueKind.True)
                    {
                        completedModulesCount++;
                    }
                    if (mod.TryGetProperty("tasks", out var tElem) && tElem.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var t in tElem.EnumerateArray())
                        {
                            totalTasks++;
                            if (t.TryGetProperty("completed", out var cProp) && cProp.ValueKind == JsonValueKind.True)
                            {
                                totalCompletedTasks++;
                            }
                        }
                    }
                }
            }

            if (totalTasks == 0 || totalCompletedTasks == 0) path.Status = "Not Started";
            else if (totalCompletedTasks == totalTasks) path.Status = "Completed";
            else path.Status = "In Progress";


            await _learningPathRepository.UpdateAsync(path);

            return new LearningPathResponseDto
            {
                Id = path.Id,
                CandidateId = path.CandidateId,
                Title = path.Title,
                Status = path.Status,
                PathData = JsonDocument.Parse(path.PathData),
                CreatedAt = path.CreatedAt
            };
        }

        private static bool IsTrue(object? val)
        {
            if (val == null) return false;
            if (val is bool b) return b;
            if (val is JsonElement je)
            {
                if (je.ValueKind == JsonValueKind.True) return true;
                if (je.ValueKind == JsonValueKind.String && bool.TryParse(je.GetString(), out var res)) return res;
            }
            if (val is string s && bool.TryParse(s, out bool sb)) return sb;
            return false;
        }

        private static bool IsModuleCompleted(Dictionary<string, object> mod)
        {
            if (mod.TryGetValue("completed", out var val) && IsTrue(val))
                return true;

            if (mod.TryGetValue("tasks", out var tasksObj) && tasksObj is JsonElement tasksElem && tasksElem.ValueKind == JsonValueKind.Array)
            {
                var tasks = tasksElem.EnumerateArray().ToList();
                if (tasks.Count > 0 && tasks.All(t => t.TryGetProperty("completed", out var cv) && cv.ValueKind == JsonValueKind.True))
                {
                    return true;
                }
            }
            else if (mod.TryGetValue("tasks", out var listObj) && listObj is List<Dictionary<string, object>> list)
            {
                if (list.Count > 0 && list.All(t => t.TryGetValue("completed", out var cv) && IsTrue(cv)))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
