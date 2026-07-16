using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
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

        public LearningPathUseCase(
            ILearningPathRepository learningPathRepository,
            IInterviewAnswerRepository interviewAnswerRepository,
            IInterviewSessionRepository interviewSessionRepository,
            IAiService aiService,
            ITHunterviewContext context)
        {
            _learningPathRepository = learningPathRepository;
            _interviewAnswerRepository = interviewAnswerRepository;
            _interviewSessionRepository = interviewSessionRepository;
            _aiService = aiService;
            _context = context;
        }

        private const int MaxLearningPathsPerCandidate = 3;

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

            var template = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .ThenInclude(rs => rs.SfiaSkill)
                .FirstOrDefaultAsync(t => t.Id == request.TargetRoleTemplateId);

            if (template == null)
            {
                throw new ArgumentException("Target role template not found.");
            }

            // 1. Gap Calculation
            var gaps = new List<object>();
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
            userPromptBuilder.AppendLine($"Target Role: {template.RoleName}");
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
            var matchContext = await BuildMatchContextAsync(candidateId, matchScoreId);
            if (string.IsNullOrWhiteSpace(matchContext))
                throw new InvalidOperationException("Chưa có dữ liệu matching CV-JD.");

            return await PerformExtractionAsync(matchContext, "CV-JD Matching");
        }

        public async Task<ExtractSfiaProfileResponseDto> ExtractFromInterviewAsync(Guid candidateId, Guid sessionId)
        {
            var interviewContext = await BuildInterviewContextAsync(candidateId, sessionId);
            if (string.IsNullOrWhiteSpace(interviewContext))
                throw new InvalidOperationException("Chưa có dữ liệu phỏng vấn thử.");

            return await PerformExtractionAsync(interviewContext, "Mock Interview");
        }

        private async Task<ExtractSfiaProfileResponseDto> PerformExtractionAsync(string contextText, string sourceName)
        {
            var templates = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .ThenInclude(rs => rs.SfiaSkill)
                .ToListAsync();

            var templateListJson = JsonSerializer.Serialize(templates.Select(t => new
            {
                t.Id,
                t.RoleName,
                RequiredSkills = t.RequiredSkills.Select(rs => new { rs.SfiaSkill.SkillCode, rs.SfiaSkill.SkillName })
            }));

            var allSkills = await _context.SfiaSkills.ToListAsync();
            var allSkillsJson = JsonSerializer.Serialize(allSkills.Select(s => new { s.SkillCode, s.SkillName }));

            string systemPrompt = @"You are an expert IT career coach and data extractor.
Analyze the candidate's skill gaps identified from their " + sourceName + @" context.
You will be provided a list of available Target Role Templates and their Required Skills, AND a list of all 147 SFIA skills.
Your job is to:
1. Select the BEST matching targetRoleTemplateId from the provided list that fits the candidate's context.
2. If AND ONLY IF absolutely no role from the list matches the candidate's context (e.g., highly specialized), leave targetRoleTemplateId null and generate a `newRole` by defining its name, description, and required skills from the SFIA list.
3. For EACH required skill (either from the selected role OR the newly generated role), estimate the candidate's CURRENT proficiency level (from 0 to 7, where 0 is no experience, 1-7 are SFIA levels). Use the context to make an educated guess.
The result MUST be a valid JSON object strictly following this schema:
{
  ""targetRoleTemplateId"": ""GUID_HERE"" | null,
  ""newRole"": null | {
      ""roleName"": ""..."",
      ""description"": ""..."",
      ""requiredSkills"": [ { ""skillCode"": ""..."", ""targetLevel"": 4 } ]
  },
  ""currentSkills"": [
    { ""skillCode"": ""..."", ""currentLevel"": 3 }
  ]
}
Do NOT include any markdown blocks like ```json, just return the raw JSON object.";

            var userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine("=== AVAILABLE TARGET ROLE TEMPLATES ===");
            userPromptBuilder.AppendLine(templateListJson);
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("=== ALL SFIA SKILLS ===");
            userPromptBuilder.AppendLine(allSkillsJson);
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

                if (dto.TargetRoleTemplateId == null && dto.NewRole != null)
                {
                    var newRole = new TargetRoleTemplate
                    {
                        Id = Guid.NewGuid(),
                        RoleName = dto.NewRole.RoleName,
                        Description = dto.NewRole.Description,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    
                    foreach(var rs in dto.NewRole.RequiredSkills)
                    {
                        var dbSkill = await _context.SfiaSkills.FirstOrDefaultAsync(s => s.SkillCode == rs.SkillCode);
                        if (dbSkill != null)
                        {
                            newRole.RequiredSkills.Add(new TargetRoleSkill
                            {
                                Id = Guid.NewGuid(),
                                RoleTemplateId = newRole.Id,
                                SfiaSkillId = dbSkill.Id,
                                TargetLevel = rs.TargetLevel
                            });
                        }
                    }

                    _context.TargetRoleTemplates.Add(newRole);
                    await _context.SaveChangesAsync();

                    dto.TargetRoleTemplateId = newRole.Id;
                }

                if (dto.TargetRoleTemplateId == null || dto.TargetRoleTemplateId == Guid.Empty)
                {
                    throw new Exception("AI failed to return a valid Target Role.");
                }

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
            var existingPaths = await _learningPathRepository.GetByCandidateIdAsync(candidateId);
            if (existingPaths.Count >= MaxLearningPathsPerCandidate)
            {
                throw new InvalidOperationException(
                    $"Bạn đã đạt giới hạn tối đa {MaxLearningPathsPerCandidate} lộ trình học. " +
                    "Vui lòng xoá một lộ trình cũ trước khi tạo lộ trình mới.");
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

            sb.AppendLine($"Overall Match Score: {matchRecord.MatchScore:F1}/100");

            // Trích thông tin từ MatchDetails JSON (LLM Bypass format)
            try
            {
                using var doc = JsonDocument.Parse(matchRecord.MatchDetails);
                var root = doc.RootElement;

                if (root.TryGetProperty("jdFit", out var jdFit))
                {
                    // Pool A - Technical skills
                    if (jdFit.TryGetProperty("poolA", out var poolA))
                    {
                        if (poolA.TryGetProperty("missingSkills", out var missingSkills))
                            sb.AppendLine($"Missing Technical Skills: {missingSkills}");

                        if (poolA.TryGetProperty("weakSkills", out var weakSkills))
                            sb.AppendLine($"Weak Technical Skills: {weakSkills}");

                        if (poolA.TryGetProperty("score", out var poolAScore))
                            sb.AppendLine($"Technical Skills Score: {poolAScore}/40");
                    }

                    // Pool B - Soft skills / experience
                    if (jdFit.TryGetProperty("poolB", out var poolB))
                    {
                        if (poolB.TryGetProperty("gaps", out var gaps))
                            sb.AppendLine($"Experience/Soft Skill Gaps: {gaps}");

                        if (poolB.TryGetProperty("score", out var poolBScore))
                            sb.AppendLine($"Experience/Soft Skill Score: {poolBScore}/60");
                    }

                    // Penalties
                    if (jdFit.TryGetProperty("penalties", out var penalties) &&
                        penalties.ValueKind == JsonValueKind.Array)
                    {
                        var triggeredPenalties = penalties.EnumerateArray()
                            .Where(p => p.TryGetProperty("triggered", out var t) && t.GetBoolean())
                            .Select(p => p.TryGetProperty("reason", out var r) ? r.GetString() : null)
                            .Where(r => r != null)
                            .ToList();

                        if (triggeredPenalties.Any())
                            sb.AppendLine($"Penalty Reasons: {string.Join("; ", triggeredPenalties)}");
                    }
                }
                else if (root.TryGetProperty("Method", out var methodProp) && methodProp.GetString() == "Hardcode")
                {
                    sb.AppendLine("Matching Method: Keyword-based (Hardcode)");
                    if (root.TryGetProperty("TitleScore", out var titleScore)) sb.AppendLine($"Title Score: {titleScore}");
                    if (root.TryGetProperty("SkillsScore", out var skillsScore)) sb.AppendLine($"Skills Score: {skillsScore}");
                    if (root.TryGetProperty("ExperienceScore", out var expScore)) sb.AppendLine($"Experience Score: {expScore}");
                    if (root.TryGetProperty("DomainScore", out var domainScore)) sb.AppendLine($"Domain Score: {domainScore}");
                    sb.AppendLine();
                    sb.AppendLine("Note: Keyword-based matching does not provide specific missing skills. The AI will generate a general path based on the target role.");
                }
                else
                {
                    sb.AppendLine("Match Details: (Custom format)");
                    // Avoid dumping raw JSON to the UI
                }
            }
            catch
            {
                // JSON không parse được → dùng raw text
                sb.AppendLine($"Match Details: {matchRecord.MatchDetails}");
            }

            return sb.ToString();
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
            bool currentTaskStatus = false;
            if (tasksList[taskIndex].TryGetValue("completed", out var compVal) && compVal is JsonElement compElem)
            {
                currentTaskStatus = compElem.ValueKind == JsonValueKind.True;
            }

            // Determine intended next status
            bool nextStatus = !currentTaskStatus;

            // Enforcement Rules
            if (nextStatus) // Checking
            {
                // Check previous module is completed
                if (moduleIndex > 0)
                {
                    if (modules[moduleIndex - 1].TryGetValue("completed", out var prevModVal) && prevModVal is JsonElement prevModElem)
                    {
                        if (prevModElem.ValueKind != JsonValueKind.True)
                            throw new ArgumentException("You must complete the previous module first.");
                    }
                }

                // Check previous task in current module is completed
                if (taskIndex > 0)
                {
                    if (tasksList[taskIndex - 1].TryGetValue("completed", out var prevTaskVal) && prevTaskVal is JsonElement prevTaskElem)
                    {
                        if (prevTaskElem.ValueKind != JsonValueKind.True)
                            throw new ArgumentException("You must complete the previous task first.");
                    }
                }
            }
            else // Unchecking
            {
                // Ensure next task in current module is not completed
                if (taskIndex < tasksList.Count - 1)
                {
                    if (tasksList[taskIndex + 1].TryGetValue("completed", out var nextTaskVal) && nextTaskVal is JsonElement nextTaskElem)
                    {
                        if (nextTaskElem.ValueKind == JsonValueKind.True)
                            throw new ArgumentException("Cannot uncheck task because the subsequent task is already completed.");
                    }
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

            // Update module completion status based on its tasks
            bool isModuleCompleted = tasksList.All(t => t.TryGetValue("completed", out var cv) && cv is JsonElement ce && ce.ValueKind == JsonValueKind.True);
            modules[moduleIndex]["completed"] = isModuleCompleted;

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
    }
}
