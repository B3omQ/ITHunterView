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
        // Generate từ input thủ công (giữ nguyên)
        // ─────────────────────────────────────────────────────────────
        public async Task<LearningPathResponseDto> GenerateLearningPathAsync(Guid candidateId, GeneratePathRequestDto request)
        {
            await EnforceMaxPathsAsync(candidateId);

            string systemPrompt = @"You are an expert IT career coach. 
Generate a comprehensive, step-by-step learning path based on the user's current skills and target role.
The result MUST be a valid JSON array of objects, where each object represents a learning module.
Example output format:
[
  {
    ""title"": ""Module 1: Introduction"",
    ""description"": ""Basic concepts."",
    ""durationWeeks"": 2,
    ""skills"": [""Skill A"", ""Skill B""]
  }
]
Do NOT include any markdown blocks like ```json, just return the raw JSON array.";

            string userPrompt = $@"
Target Role: {request.TargetRole}
Current Skills: {request.CurrentSkills}
Target Skills: {request.TargetSkills}
Desired Timeframe: {request.TimeframeInWeeks} weeks.

Please generate a structured learning path.";

            return await CallAiAndSaveAsync(candidateId, userPrompt, systemPrompt);
        }

        // ─────────────────────────────────────────────────────────────
        // Generate từ lịch sử matching CV-JD & phỏng vấn
        // ─────────────────────────────────────────────────────────────
        public async Task<LearningPathResponseDto> GenerateFromCvJdAsync(Guid candidateId, GenerateFromCvJdRequestDto request)
        {
            await EnforceMaxPathsAsync(candidateId);

            var matchContext = await BuildMatchContextAsync(candidateId, request.MatchScoreId);

            if (string.IsNullOrWhiteSpace(matchContext))
            {
                throw new InvalidOperationException(
                    "Chưa có dữ liệu matching CV-JD để tạo lộ trình. " +
                    "Vui lòng thực hiện matching CV-JD trước.");
            }

            string systemPrompt = @"You are an expert IT career coach.
Analyze the candidate's skill gaps identified from their CV-JD matching results.
Generate a targeted, step-by-step learning path to close those specific gaps.
The result MUST be a valid JSON array of objects, where each object represents a learning module.
Each module must directly address one or more identified skill gaps.
Example output format:
[
  {
    ""title"": ""Module 1: Gap Topic"",
    ""description"": ""What to learn and why it closes the gap."",
    ""durationWeeks"": 2,
    ""skills"": [""Skill A"", ""Skill B""],
    ""gapSource"": ""cv-jd-match""
  }
]
Do NOT include any markdown blocks like ```json, just return the raw JSON array.";

            var userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine($"Desired Timeframe: {request.TimeframeInWeeks} weeks.");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("=== SKILL GAPS FROM CV-JD MATCHING ===");
            userPromptBuilder.AppendLine(matchContext);
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("Based on the above identified skill gaps, generate a prioritized learning path.");

            return await CallAiAndSaveAsync(candidateId, userPromptBuilder.ToString(), systemPrompt);
        }

        public async Task<LearningPathResponseDto> GenerateFromInterviewAsync(Guid candidateId, GenerateFromInterviewRequestDto request)
        {
            await EnforceMaxPathsAsync(candidateId);

            var interviewContext = await BuildInterviewContextAsync(candidateId, request.SessionId);

            if (string.IsNullOrWhiteSpace(interviewContext))
            {
                throw new InvalidOperationException(
                    "Chưa có dữ liệu phỏng vấn thử để tạo lộ trình. " +
                    "Vui lòng thực hiện phỏng vấn thử trước.");
            }

            string systemPrompt = @"You are an expert IT career coach.
Analyze the candidate's weak areas identified from their mock interview performance.
Generate a targeted, step-by-step learning path to close those specific gaps.
The result MUST be a valid JSON array of objects, where each object represents a learning module.
Each module must directly address one or more identified skill gaps.
Example output format:
[
  {
    ""title"": ""Module 1: Gap Topic"",
    ""description"": ""What to learn and why it closes the gap."",
    ""durationWeeks"": 2,
    ""skills"": [""Skill A"", ""Skill B""],
    ""gapSource"": ""interview""
  }
]
Do NOT include any markdown blocks like ```json, just return the raw JSON array.";

            var userPromptBuilder = new StringBuilder();
            userPromptBuilder.AppendLine($"Desired Timeframe: {request.TimeframeInWeeks} weeks.");
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("=== WEAK AREAS FROM MOCK INTERVIEW ===");
            userPromptBuilder.AppendLine(interviewContext);
            userPromptBuilder.AppendLine();
            userPromptBuilder.AppendLine("Based on the above identified weak areas, generate a prioritized learning path.");

            return await CallAiAndSaveAsync(candidateId, userPromptBuilder.ToString(), systemPrompt);
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

            JsonDocument jsonDoc;
            try
            {
                jsonDoc = JsonDocument.Parse(aiResponseText);
            }
            catch (Exception)
            {
                throw new Exception("AI generated an invalid JSON response. Please try again.");
            }

            var learningPath = new LearningPaths
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                PathData = aiResponseText,
                CreatedAt = DateTime.UtcNow
            };

            await _learningPathRepository.AddAsync(learningPath);

            return new LearningPathResponseDto
            {
                Id = learningPath.Id,
                CandidateId = learningPath.CandidateId,
                PathData = jsonDoc,
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
    }
}
