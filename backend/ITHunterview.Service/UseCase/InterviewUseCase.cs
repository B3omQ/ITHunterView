using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Interview;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;
using ITHunterview.Service.Infrastructure.Persistence;

namespace ITHunterview.Service.UseCase
{
    public class InterviewUseCase : IInterviewUseCase
    {
        private readonly IInterviewSessionRepository _sessionRepository;
        private readonly IInterviewAnswerRepository _answerRepository;
        private readonly ICvRepository _cvRepository;
        private readonly IJobPostingRepository _jobPostingRepository;
        private readonly IAiService _aiService;
        private readonly ICvTextExtractorService _cvTextExtractorService;
        private readonly IPromptManagementService _promptManagementService;
        private readonly ITHunterviewContext _context;

        public InterviewUseCase(
            IInterviewSessionRepository sessionRepository,
            IInterviewAnswerRepository answerRepository,
            ICvRepository cvRepository,
            IJobPostingRepository jobPostingRepository,
            IAiService aiService,
            ICvTextExtractorService cvTextExtractorService,
            IPromptManagementService promptManagementService,
            ITHunterviewContext context)
        {
            _sessionRepository = sessionRepository;
            _answerRepository = answerRepository;
            _cvRepository = cvRepository;
            _jobPostingRepository = jobPostingRepository;
            _aiService = aiService;
            _cvTextExtractorService = cvTextExtractorService;
            _promptManagementService = promptManagementService;
            _context = context;
        }

        public async Task<List<InterviewSessionDto>> GetCandidateSessionsAsync(Guid candidateId)
        {
            var sessions = await _sessionRepository.GetByCandidateIdAsync(candidateId);
            var result = new List<InterviewSessionDto>();

            foreach (var session in sessions)
            {
                string? jobTitle = null;
                if (session.JobId.HasValue)
                {
                    var job = await _jobPostingRepository.GetByIdAsync(session.JobId.Value);
                    jobTitle = job?.Title;
                }

                string? cvName = null;
                if (session.CvId.HasValue)
                {
                    var cv = await _cvRepository.GetByIdAsync(session.CvId.Value);
                    cvName = cv?.FileName;
                }

                result.Add(new InterviewSessionDto
                {
                    Id = session.Id,
                    CandidateId = session.CandidateId,
                    JobId = session.JobId,
                    JobTitle = jobTitle,
                    CvId = session.CvId,
                    CvFileName = cvName,
                    DifficultyLevel = session.DifficultyLevel,
                    Status = session.Status,
                    StartedAt = session.StartedAt,
                    EndedAt = session.EndedAt,
                    AiProvider = session.AiProvider,
                    Language = session.Language
                });
            }

            return result;
        }

        public async Task<InterviewSessionDetailDto> GetSessionDetailAsync(Guid sessionId, Guid candidateId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Interview session not found.");
            }

            string? jobTitle = null;
            if (session.JobId.HasValue)
            {
                var job = await _jobPostingRepository.GetByIdAsync(session.JobId.Value);
                jobTitle = job?.Title;
            }

            string? cvName = null;
            if (session.CvId.HasValue)
            {
                var cv = await _cvRepository.GetByIdAsync(session.CvId.Value);
                cvName = cv?.FileName;
            }

            var sessionDto = new InterviewSessionDto
            {
                Id = session.Id,
                CandidateId = session.CandidateId,
                JobId = session.JobId,
                JobTitle = jobTitle,
                CvId = session.CvId,
                CvFileName = cvName,
                DifficultyLevel = session.DifficultyLevel,
                Status = session.Status,
                StartedAt = session.StartedAt,
                EndedAt = session.EndedAt,
                AiProvider = session.AiProvider,
                Language = session.Language
            };

            var answers = await _answerRepository.GetBySessionIdAsync(sessionId);
            var messages = answers.Select(a => new InterviewAnswerDto
            {
                Id = a.Id,
                SessionId = a.SessionId,
                QuestionId = a.QuestionId,
                ParentAnswerId = a.ParentAnswerId,
                QuestionText = a.QuestionText,
                AudioUrl = a.AudioUrl,
                CandidateTranscript = a.CandidateTranscript,
                AiFeedback = a.AiFeedback,
                ScoreLogic = a.ScoreLogic,
                ScoreTech = a.ScoreTech,
                ScoreCommunication = a.ScoreCommunication,
                CreatedAt = a.CreatedAt
            }).ToList();

            InterviewReportDto? reportDto = null;
            if (session.Status == InterviewSessionStatus.COMPLETED)
            {
                var report = await GenerateSessionReportAsync(sessionId, candidateId);
                if (report != null)
                {
                    reportDto = new InterviewReportDto
                    {
                        Id = report.Id,
                        SessionId = report.SessionId,
                        TotalScore = report.TotalScore,
                        OverallFeedback = report.OverallFeedback
                    };
                }
            }

            return new InterviewSessionDetailDto
            {
                Session = sessionDto,
                Messages = messages,
                Report = reportDto
            };
        }

        public async Task<InterviewSessionDto> CreateSessionAsync(Guid candidateId, CreateInterviewSessionDto dto)
        {
            // Determine active provider
            var provider = string.IsNullOrWhiteSpace(dto.AiProvider)
                ? await _aiService.GetActiveProviderNameAsync()
                : dto.AiProvider;

            // Fetch Job details first if JobId is provided to determine DifficultyLevel
            string jobContext = "Chưa có thông tin công việc (JD).";
            string jobTitle = "";
            var resolvedDifficulty = dto.DifficultyLevel;
            string exactLevel = resolvedDifficulty switch
            {
                DifficultyLevel.EASY => "Intern / Fresher",
                DifficultyLevel.MEDIUM => "Middle",
                DifficultyLevel.HARD => "Senior",
                _ => "Junior"
            };

            if (dto.JobId.HasValue)
            {
                var job = await _jobPostingRepository.GetByIdAsync(dto.JobId.Value);
                if (job != null)
                {
                    jobTitle = job.Title ?? "";
                    jobContext = $"Title: {job.Title}\nDescription: {job.Description}\nRequirements: {job.Requirements}";
                    
                    if (!string.IsNullOrWhiteSpace(job.Level))
                    {
                        string lvl = job.Level.ToLower();
                        if (lvl.Contains("intern") || lvl.Contains("fresher"))
                        {
                            resolvedDifficulty = DifficultyLevel.EASY;
                            exactLevel = "Intern / Fresher";
                        }
                        else if (lvl.Contains("junior"))
                        {
                            resolvedDifficulty = DifficultyLevel.EASY;
                            exactLevel = "Junior";
                        }
                        else if (lvl.Contains("senior") || lvl.Contains("lead") || lvl.Contains("architect") || lvl.Contains("principal"))
                        {
                            resolvedDifficulty = DifficultyLevel.HARD;
                            exactLevel = "Senior";
                        }
                        else
                        {
                            resolvedDifficulty = DifficultyLevel.MEDIUM;
                            exactLevel = "Middle";
                        }
                    }
                    else
                    {
                        resolvedDifficulty = DifficultyLevel.MEDIUM; // Default if level is empty in JD
                        exactLevel = "Middle";
                    }
                }
            }

            string sessionLanguage = (dto.Language ?? "vi").ToLower() == "en" ? "en" : "vi";

            var session = new InterviewSessions
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                JobId = dto.JobId,
                CvId = dto.CvId,
                DifficultyLevel = resolvedDifficulty,
                Status = InterviewSessionStatus.IN_PROGRESS,
                StartedAt = DateTime.UtcNow,
                AiProvider = provider,
                Language = sessionLanguage
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();

            // Fetch context CV details to inject in prompt
            string cvContext = "Chưa có thông tin CV.";
            string cvFileName = "";
            Cvs? cv = null;
            if (dto.CvId.HasValue)
            {
                cv = await _cvRepository.GetByIdAsync(dto.CvId.Value);
                if (cv != null)
                {
                    cvFileName = cv.FileName ?? "";
                    if (!string.IsNullOrWhiteSpace(cv.RawText))
                    {
                        cvContext = cv.RawText;
                    }
                    else if (!string.IsNullOrWhiteSpace(cv.ParsedData))
                    {
                        cvContext = cv.ParsedData;
                    }
                    else if (!string.IsNullOrWhiteSpace(cv.FileUrl))
                    {
                        try
                        {
                            Console.WriteLine($"[INFO] CV RawText is empty. Extracting text from URL: {cv.FileUrl}");
                            var extractedText = await _cvTextExtractorService.ExtractTextFromUrlAsync(cv.FileUrl);
                            if (!string.IsNullOrWhiteSpace(extractedText))
                            {
                                cv.RawText = extractedText;
                                await _cvRepository.UpdateAsync(cv);
                                cvContext = extractedText;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Failed to extract CV text in CreateSessionAsync: {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("================ [LOG TERMINAL: CV INFORMATION RECEIVED] ================");
            Console.WriteLine($"CV FileName: {cvFileName}");
            Console.WriteLine($"CV Content Preview (Length: {cvContext.Length} chars):");
            Console.WriteLine(cvContext.Length > 1000 ? cvContext.Substring(0, 1000) + "\n...[TRUNCATED FOR LOG]..." : cvContext);
            Console.WriteLine("=========================================================================");

            // Phân loại Role & Seniority và trích xuất câu hỏi mẫu từ Rubric
            string role = DetermineRole(jobTitle, cvFileName, cvContext);
            var sampleQuestions = await GetSampleQuestionsAsync(role, exactLevel);
            string rubricContext = sampleQuestions.Count > 0
                ? "Dưới đây là một số câu hỏi mẫu từ bộ quy chuẩn đánh giá của ITHunterView để bạn tham khảo phong cách, độ khó và nội dung:\n- " + string.Join("\n- ", sampleQuestions)
                : "";

            int totalQuestions = resolvedDifficulty == DifficultyLevel.HARD ? 8 : 7;

            string langInstruction = sessionLanguage == "en"
                ? "BẮT BUỘC NGÔN NGỮ: Bạn PHẢI thực hiện toàn bộ buổi phỏng vấn bằng TIẾNG ANH (English). Đặt tất cả câu hỏi và trả lời hoàn toàn bằng tiếng Anh."
                : "BẮT BUỘC NGÔN NGỮ: Bạn PHẢI thực hiện toàn bộ buổi phỏng vấn bằng tiếng Việt.";

            var systemPrompt = $"Bạn là một người phỏng vấn IT tuyển dụng chuyên nghiệp. Nhiệm vụ của bạn là thực hiện một buổi phỏng vấn thử (mock interview) gồm đúng {totalQuestions} câu hỏi cho cấp độ ứng viên: {exactLevel} (Role: {role}).\n\n" +
                               $"{langInstruction}\n\n" +
                               $"LỘ TRÌNH PHỎNG VẤN:\n" +
                               $"Phần 1: Giới thiệu bản thân (Câu 1)\n" +
                               $"Phần 2: Câu hỏi kiến thức\n" +
                               $"Phần 3: Câu hỏi kinh nghiệm & dự án\n" +
                               $"Phần 4: Kỹ năng mềm / Xử lý tình huống\n" +
                               $"Phần 5: Hiểu biết về công ty (Câu {totalQuestions})\n\n" +
                               $"THÔNG TIN BỐ CẢNH:\n" +
                               $"--- START CV ---\n{cvContext}\n--- END CV ---\n\n" +
                               $"--- START JD ---\n{jobContext}\n--- END JD ---\n\n" +
                               $"{rubricContext}\n\n" +
                               $"QUY TẮC PHÂN LOẠI DẠNG CÂU HỎI THEO LEVEL:\n" +
                               $"Bạn phải đặt câu hỏi tuân thủ nghiêm ngặt theo dạng câu hỏi (đóng/mở/outside-the-box) dựa trên Cấp độ phỏng vấn hiện tại:\n" +
                               $"1. Đối với Intern/Fresher và Junior:\n" +
                               $"   - Phần 2 (Kiến thức): Sử dụng câu hỏi đóng (closed-ended) để kiểm tra nhanh lý thuyết nền tảng cơ bản.\n" +
                               $"   - Phần 3 & 4 (Kinh nghiệm & Kỹ năng): Sử dụng câu hỏi mở (open-ended).\n" +
                               $"   - Tuyệt đối KHÔNG sử dụng câu hỏi \"Outside-the-box\" hay câu hỏi tình huống quá phức tạp.\n" +
                               $"2. Đối với Middle:\n" +
                               $"   - Phần 2 (Kiến thức): Sử dụng câu hỏi mở (open-ended) để khai thác sâu tư duy áp dụng thực tế.\n" +
                               $"   - Phần 3 & 4: Sử dụng câu hỏi mở (open-ended).\n" +
                               $"   - KHÔNG sử dụng câu hỏi \"Outside-the-box\".\n" +
                               $"3. Đối với Senior:\n" +
                               $"   - Phần 2 (Kiến thức): Sử dụng câu hỏi mở (open-ended) tập trung vào so sánh công nghệ và trade-off thiết kế (system design).\n" +
                               $"   - Phần 3: Đào sâu dự án phức tạp, vai trò lãnh đạo/mentor.\n" +
                               $"   - Phần 4 (Kỹ năng/Tình huống): Kết hợp câu hỏi mở và câu hỏi \"Outside-the-box\" (tư duy sáng tạo, tình huống phi truyền thống hoàn toàn mới).\n\n" +
                               $"NGUYÊN TẮC KHÔNG LEO THANG ĐỘ KHÓ TỰ ĐỘNG:\n" +
                               $"- Độ khó và dạng câu hỏi phải cố định theo level đã xác định từ đầu ({exactLevel}).\n" +
                               $"- Tuyệt đối không tự ý đổi dạng câu hỏi sang Outside-the-box hoặc tăng độ khó vượt quá kỳ vọng năng lực của level hiện tại chỉ vì ứng viên trả lời tốt các câu hỏi trước đó.\n\n" +
                               $"LƯU Ý QUAN TRỌNG VỀ TÌNH HUỐNG LỆCH CÔNG NGHỆ:\n" +
                               $"- Hãy đối chiếu kỹ CV và JD. Nếu có sự lệch công nghệ lớn (ví dụ: JD yêu cầu .NET nhưng CV chỉ có Java), bạn PHẢI nhận biết được điều này và chuẩn bị các câu hỏi tình huống thích ứng công nghệ mới ở các câu tiếp theo.\n\n" +
                               $"YÊU CẦU CHO CÂU HỎI 1:\n" +
                               $"- Đây là câu hỏi số 1/{totalQuestions} (Chủ đề: Phần 1 - Giới thiệu bản thân).\n" +
                               $"- Hãy bắt đầu bằng lời chào mừng ứng viên ứng tuyển vào vị trí (được ghi trong JD) từ hệ thống ITHunterView, sau đó mời ứng viên giới thiệu bản thân.\n" +
                               $"- Chỉ hỏi DUY NHẤT một câu hỏi chính trong mỗi lượt chat.\n" +
                               (sessionLanguage == "en" ? "- Trả lời ngắn gọn bằng tiếng Anh (English)." : "- Trả lời ngắn gọn bằng tiếng Việt.");

            string candidateName = "";
            if (cv != null && !string.IsNullOrWhiteSpace(cv.ParsedData))
            {
                try
                {
                    using var doc = JsonDocument.Parse(cv.ParsedData);
                    if (doc.RootElement.TryGetProperty("personal_info", out var personalInfo) &&
                        personalInfo.TryGetProperty("name", out var nameProp))
                    {
                        candidateName = nameProp.GetString() ?? "";
                    }
                }
                catch { }
            }

            string positionName = !string.IsNullOrWhiteSpace(jobTitle) ? jobTitle : (role + " Developer");

            string firstQuestion;
            if (sessionLanguage == "en")
            {
                string greetingPartEn = !string.IsNullOrWhiteSpace(candidateName)
                    ? $"Hello {candidateName.Trim()}"
                    : "Hello";
                firstQuestion = $"{greetingPartEn}, thank you for joining the interview session for the {positionName} position today.\n\n" +
                                $"Our interview roadmap consists of {totalQuestions} questions covering:\n" +
                                $"- Part 1: Self-Introduction (Question 1)\n" +
                                $"- Part 2: Technical Knowledge\n" +
                                $"- Part 3: Experience & Projects\n" +
                                $"- Part 4: Soft Skills / Scenario Handling\n" +
                                $"- Part 5: Company Understanding (Question {totalQuestions})\n\n" +
                                $"To get started, please introduce yourself and highlight your relevant work or academic background.";
            }
            else
            {
                string greetingPart = !string.IsNullOrWhiteSpace(candidateName)
                    ? $"Chào bạn {candidateName.Trim()}"
                    : "Chào bạn";
                firstQuestion = $"{greetingPart}, cảm ơn bạn đã tham gia buổi phỏng vấn cho vị trí {positionName} ngày hôm nay.\n\n" +
                                $"Lộ trình phỏng vấn của chúng ta sẽ gồm {totalQuestions} câu hỏi đi qua các phần:\n" +
                                $"- Phần 1: Giới thiệu bản thân (Câu 1)\n" +
                                $"- Phần 2: Câu hỏi kiến thức\n" +
                                $"- Phần 3: Câu hỏi kinh nghiệm & dự án\n" +
                                $"- Phần 4: Kỹ năng mềm / Xử lý tình huống\n" +
                                $"- Phần 5: Hiểu biết về công ty (Câu {totalQuestions})\n\n" +
                                $"Để bắt đầu, bạn hãy giới thiệu đôi chút về bản thân và những kinh nghiệm làm việc/học tập nổi bật của mình nhé.";
            }

            var firstTurn = new InterviewAnswers
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                QuestionText = firstQuestion,
                CreatedAt = DateTime.UtcNow
            };

            await _answerRepository.AddAsync(firstTurn);
            await _answerRepository.SaveChangesAsync();

            return new InterviewSessionDto
            {
                Id = session.Id,
                CandidateId = session.CandidateId,
                JobId = session.JobId,
                CvId = session.CvId,
                DifficultyLevel = session.DifficultyLevel,
                Status = session.Status,
                StartedAt = session.StartedAt,
                AiProvider = session.AiProvider,
                Language = session.Language
            };
        }

        public async Task<InterviewAnswerDto> SubmitReplyAsync(Guid sessionId, Guid candidateId, SubmitReplyDto dto)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Session not found or unauthorized.");
            }

            if (session.Status != InterviewSessionStatus.IN_PROGRESS)
            {
                throw new InvalidOperationException("This interview session has already been completed.");
            }

            var activeTurn = await _answerRepository.GetActiveTurnAsync(sessionId);
            if (activeTurn == null)
            {
                throw new InvalidOperationException("No active question waiting for response.");
            }

            // Fetch context CV / Job details to inject in prompt
            string cvContext = "Chưa có thông tin CV.";
            string cvFileName = "";
            if (session.CvId.HasValue)
            {
                var cv = await _cvRepository.GetByIdAsync(session.CvId.Value);
                if (cv != null)
                {
                    cvFileName = cv.FileName ?? "";
                    if (!string.IsNullOrWhiteSpace(cv.RawText))
                    {
                        cvContext = cv.RawText;
                    }
                    else if (!string.IsNullOrWhiteSpace(cv.ParsedData))
                    {
                        cvContext = cv.ParsedData;
                    }
                    else if (!string.IsNullOrWhiteSpace(cv.FileUrl))
                    {
                        try
                        {
                            Console.WriteLine($"[INFO] CV RawText is empty. Extracting text from URL: {cv.FileUrl}");
                            var extractedText = await _cvTextExtractorService.ExtractTextFromUrlAsync(cv.FileUrl);
                            if (!string.IsNullOrWhiteSpace(extractedText))
                            {
                                cv.RawText = extractedText;
                                await _cvRepository.UpdateAsync(cv);
                                cvContext = extractedText;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[ERROR] Failed to extract CV text in SubmitReplyAsync: {ex.Message}");
                        }
                    }
                }
            }

            Console.WriteLine("================ [LOG TERMINAL: CV INFORMATION RECEIVED] ================");
            Console.WriteLine($"CV FileName: {cvFileName}");
            Console.WriteLine($"CV Content Preview (Length: {cvContext.Length} chars):");
            Console.WriteLine(cvContext.Length > 1000 ? cvContext.Substring(0, 1000) + "\n...[TRUNCATED FOR LOG]..." : cvContext);
            Console.WriteLine("=========================================================================");

            string jobContext = "Chưa có thông tin công việc (JD).";
            string jobTitle = "";
            string jobLevel = "";
            if (session.JobId.HasValue)
            {
                var job = await _jobPostingRepository.GetByIdAsync(session.JobId.Value);
                if (job != null)
                {
                    jobTitle = job.Title ?? "";
                    jobLevel = job.Level ?? "";
                    jobContext = $"Title: {job.Title}\nDescription: {job.Description}\nRequirements: {job.Requirements}";
                }
            }

            string exactLevel = "Junior";
            if (session.DifficultyLevel == DifficultyLevel.EASY)
            {
                exactLevel = "Intern / Fresher";
                if (!string.IsNullOrWhiteSpace(jobLevel) && jobLevel.ToLower().Contains("junior"))
                {
                    exactLevel = "Junior";
                }
            }
            else if (session.DifficultyLevel == DifficultyLevel.MEDIUM)
            {
                exactLevel = "Middle";
            }
            else if (session.DifficultyLevel == DifficultyLevel.HARD)
            {
                exactLevel = "Senior";
            }

            int totalQuestions = session.DifficultyLevel == DifficultyLevel.HARD ? 8 : 7;

            // Phân loại Role & Seniority và trích xuất câu hỏi mẫu từ Rubric
            string role = DetermineRole(jobTitle, cvFileName, cvContext);
            var sampleQuestions = await GetSampleQuestionsAsync(role, exactLevel);
            string rubricContext = sampleQuestions.Count > 0
                ? "Dưới đây là một số câu hỏi mẫu từ bộ quy chuẩn đánh giá của ITHunterView để bạn tham khảo phong cách, độ khó và nội dung:\n- " + string.Join("\n- ", sampleQuestions)
                : "";

            // Fetch previous turns for context
            var history = await _answerRepository.GetBySessionIdAsync(sessionId);
            int questionIndex = history.Count; // Số câu hỏi đã được hỏi & trả lời (tính cả câu vừa trả lời)

            // Build conversation history in-memory (using the unsaved dto.Message for the active turn)
            var historyLines = new System.Collections.Generic.List<string>();
            foreach (var h in history)
            {
                if (h.Id == activeTurn.Id)
                {
                    historyLines.Add($"AI Question: {h.QuestionText}\nCandidate Answer: {dto.Message}");
                }
                else
                {
                    historyLines.Add($"AI Question: {h.QuestionText}\nCandidate Answer: {h.CandidateTranscript ?? "(Chưa trả lời)"}");
                }
            }
            var historyText = string.Join("\n\n", historyLines);

            // exactLevel and totalQuestions are declared above sampleQuestions loading

            // Định nghĩa hướng dẫn động cho từng câu hỏi tiếp theo
            string questionInstruction = "QUY TẮC QUAN TRỌNG: Mọi câu hỏi bạn đặt ra BẮT BUỘC phải dựa trên bối cảnh thực tế từ CV của ứng viên hoặc yêu cầu của JD. TUYỆT ĐỐI KHÔNG hỏi các câu lý thuyết chung chung như trong sách giáo khoa nếu không liên kết với một kỹ năng/dự án trong CV. Bạn bắt buộc hỏi thêm 1 đến 2 câu follow up nếu ứng viên trả lời chưa rõ câu hỏi.\n\n";
            
            if (questionIndex >= totalQuestions)
            {
                questionInstruction = "ĐÂY LÀ LƯỢT ĐÁNH GIÁ CUỐI CÙNG (Buổi phỏng vấn kết thúc).\n" +
                                      $"- Ứng viên đã hoàn thành toàn bộ {totalQuestions} câu hỏi.\n" +
                                      "- Nhận xét chi tiết và mang tính xây dựng tổng quát cho toàn bộ buổi phỏng vấn (ở trường 'general_feedback').\n" +
                                      "- Ở trường 'next_question', hãy trả về câu chào tạm biệt lịch sự từ hệ thống ITHunterView và thông báo rằng buổi phỏng vấn thử đã kết thúc thành công.";
            }
            else
            {
                string currentSection = "";
                string sectionInstruction = "";

                if (session.DifficultyLevel == DifficultyLevel.EASY)
                {
                    if (questionIndex >= 1 && questionIndex <= 3) {
                        currentSection = "Phần 2 - Câu hỏi kiến thức";
                        sectionInstruction = "Hãy đặt một câu kiểm tra kiến thức chuyên môn, ưu tiên nền tảng lý thuyết cơ bản.";
                    } else if (questionIndex == 4) {
                        currentSection = "Phần 3 - Câu hỏi kinh nghiệm & dự án";
                        sectionInstruction = "Hãy hỏi về đồ án, bài tập lớn, quá trình tự học hoặc dự án thực tế trong CV.";
                    } else if (questionIndex == 5) {
                        currentSection = "Phần 4 - Kỹ năng mềm / Xử lý tình huống";
                        sectionInstruction = "Đánh giá khả năng làm việc nhóm, xử lý vấn đề cơ bản trong công việc/đồ án.";
                    } else if (questionIndex == 6) {
                        currentSection = "Phần 5 - Hiểu biết về công ty";
                        sectionInstruction = "Đánh giá mức độ tìm hiểu và sự phù hợp của ứng viên với công ty.";
                    }
                }
                else if (session.DifficultyLevel == DifficultyLevel.MEDIUM)
                {
                    if (questionIndex == 1 || questionIndex == 2) {
                        currentSection = "Phần 2 - Câu hỏi kiến thức";
                        sectionInstruction = "Kiểm tra kiến thức chuyên môn, đòi hỏi ứng viên biết cách áp dụng vào thực tế công việc.";
                    } else if (questionIndex == 3 || questionIndex == 4) {
                        currentSection = "Phần 3 - Câu hỏi kinh nghiệm & dự án";
                        sectionInstruction = "Đào sâu vào kinh nghiệm thực tế, đánh giá khả năng giải quyết vấn đề và ra quyết định kỹ thuật.";
                    } else if (questionIndex == 5) {
                        currentSection = "Phần 4 - Kỹ năng mềm / Xử lý tình huống";
                        sectionInstruction = "Đánh giá kỹ năng mềm, khả năng ra quyết định và xử lý vấn đề trong môi trường làm việc.";
                    } else if (questionIndex == 6) {
                        currentSection = "Phần 5 - Hiểu biết về công ty";
                        sectionInstruction = "Đánh giá mức độ tìm hiểu, sự phù hợp với sản phẩm và văn hóa công ty.";
                    }
                }
                else // HARD -> Senior
                {
                    if (questionIndex == 1) {
                        currentSection = "Phần 2 - Câu hỏi kiến thức";
                        sectionInstruction = "Bỏ qua kiến thức cơ bản, hỏi kiến thức chuyên sâu (ví dụ: system design, trade-off kỹ thuật) hoặc lồng vào kinh nghiệm.";
                    } else if (questionIndex >= 2 && questionIndex <= 5) {
                        currentSection = "Phần 3 - Câu hỏi kinh nghiệm & dự án";
                        sectionInstruction = "Hỏi trọng tâm vào độ phức tạp của dự án đã xử lý, vai trò lãnh đạo/mentor, khả năng ra quyết định chiến lược.";
                    } else if (questionIndex == 6) {
                        currentSection = "Phần 4 - Kỹ năng mềm / Xử lý tình huống";
                        sectionInstruction = "Đánh giá kỹ năng mềm ở mức độ Senior: quản lý rủi ro, giải quyết xung đột, tư duy chiến lược và lồng ghép câu hỏi Outside-the-box (sáng tạo).";
                    } else if (questionIndex == 7) {
                        currentSection = "Phần 5 - Hiểu biết về công ty";
                        sectionInstruction = "Hỏi về định hướng phát triển trong môi trường công ty, khả năng đóng góp vào tầm nhìn chung.";
                    }
                }

                questionInstruction += $"ĐÂY LÀ LƯỢT HỎI SỐ {questionIndex + 1}/{totalQuestions} ({currentSection}).\n" +
                                       "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                       $"- {sectionInstruction}";
            }

            string currentLang = (session.Language ?? "vi").ToLower() == "en" ? "en" : "vi";
            string langTurnInstruction = currentLang == "en"
                ? "BẮT BUỘC NGÔN NGỮ: Bạn PHẢI giao tiếp, đặt câu hỏi tiếp theo và nhận xét (general_feedback, strengths, improvements) HOÀN TOÀN BẰNG TIẾNG ANH (English)."
                : "BẮT BUỘC NGÔN NGỮ: Bạn PHẢI giao tiếp, đặt câu hỏi tiếp theo và nhận xét bằng tiếng Việt.";

            var systemPrompt = $"Bạn là một người phỏng vấn IT tuyển dụng chuyên nghiệp. Bạn đang thực hiện một buổi phỏng vấn thử với ứng viên (Cấp độ: {exactLevel}, Role: {role}).\n\n" +
                               $"{langTurnInstruction}\n\n" +
                               $"THÔNG TIN BỐ CẢNH:\n" +
                               $"--- START CV ---\n{cvContext}\n--- END CV ---\n\n" +
                               $"--- START JD ---\n{jobContext}\n--- END JD ---\n\n" +
                               $"{rubricContext}\n\n" +
                               $"QUY TẮC PHÂN LOẠI DẠNG CÂU HỎI THEO LEVEL:\n" +
                               $"Bạn phải đặt câu hỏi tuân thủ nghiêm ngặt theo dạng câu hỏi (đóng/mở/outside-the-box) dựa trên Cấp độ phỏng vấn hiện tại:\n" +
                               $"1. Đối với Intern/Fresher và Junior:\n" +
                               $"   - Phần 2 (Kiến thức): Sử dụng câu hỏi đóng (closed-ended) để kiểm tra nhanh lý thuyết nền tảng cơ bản.\n" +
                               $"   - Phần 3 & 4 (Kinh nghiệm & Kỹ năng): Sử dụng câu hỏi mở (open-ended).\n" +
                               $"   - Tuyệt đối KHÔNG sử dụng câu hỏi \"Outside-the-box\" hay câu hỏi tình huống quá phức tạp.\n" +
                               $"2. Đối với Middle:\n" +
                               $"   - Phần 2 (Kiến thức): Sử dụng câu hỏi mở (open-ended) để khai thác sâu tư duy áp dụng thực tế.\n" +
                               $"   - Phần 3 & 4: Sử dụng câu hỏi mở (open-ended).\n" +
                               $"   - KHÔNG sử dụng câu hỏi \"Outside-the-box\".\n" +
                               $"3. Đối với Senior:\n" +
                               $"   - Phần 2 (Kiến thức): Sử dụng câu hỏi mở (open-ended) tập trung vào so sánh công nghệ và trade-off thiết kế (system design).\n" +
                               $"   - Phần 3: Đào sâu dự án phức tạp, vai trò lãnh đạo/mentor.\n" +
                               $"   - Phần 4 (Kỹ năng/Tình huống): Kết hợp câu hỏi mở và câu hỏi \"Outside-the-box\" (tư duy sáng tạo, tình huống phi truyền thống hoàn toàn mới).\n\n" +
                               $"NGUYÊN TẮC KHÔNG LEO THANG ĐỘ KHÓ TỰ ĐỘNG:\n" +
                               $"- Độ khó và dạng câu hỏi phải cố định theo level đã xác định từ đầu ({exactLevel}).\n" +
                               $"- Tuyệt đối không tự ý đổi dạng câu hỏi sang Outside-the-box hoặc tăng độ khó vượt quá kỳ vọng năng lực của level hiện tại chỉ vì ứng viên trả lời tốt các câu hỏi trước đó.\n\n" +
                               $"HƯỚNG DẪN LƯỢT NÀY:\n" +
                               $"{questionInstruction}\n\n" +
                               "Bạn BẮT BUỘC phải trả về kết quả theo định dạng JSON duy nhất như sau:\n" +
                               "{\n" +
                               "  \"next_question\": \"Câu hỏi tiếp theo (hoặc lời tạm biệt kết thúc phỏng vấn)...\",\n" +
                               "  \"rubric_evaluation\": {\n" +
                               "    \"question_type\": \"technical | behavioral | coding | system_design\",\n" +
                               "    \"general_feedback\": \"Nhận xét chung về điểm mạnh, điểm yếu trong câu trả lời của ứng viên...\",\n" +
                               "    \"strengths\": [\"Điểm mạnh 1\", \"Điểm mạnh 2\"],\n" +
                               "    \"improvements\": [\"Điểm cần cải thiện 1\", \"Điểm cần cải thiện 2\"]\n" +
                               "  }\n" +
                               "}\n\n" +
                               "Lưu ý: Chỉ trả về JSON thuần túy, không bao bọc trong khối code markdown hay bất kỳ văn bản nào ngoài JSON.";


            var responseText = await _aiService.GenerateTextAsync(
                prompt: $"Lịch sử phỏng vấn:\n{historyText}\n\nỨng viên trả lời mới nhất: \"{dto.Message}\"",
                systemPrompt: systemPrompt,
                providerName: session.AiProvider ?? string.Empty
            );

            // Parse response
            var feedback = "Cảm ơn câu trả lời của bạn.";
            var scoreLogic = 70;
            var scoreTech = 70;
            var scoreCommunication = 70;
            var nextQuestion = "Bạn sẵn sàng cho câu hỏi tiếp theo chưa?";
            string rubricJsonStr = "";

            try
            {
                var (cleanJson, preamble) = ExtractJsonAndPreamble(responseText);



                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                // Trích xuất rubric_evaluation dưới dạng JSON string để lưu vào db
                if (root.TryGetProperty("rubric_evaluation", out var rubProp))
                {
                    rubricJsonStr = rubProp.ToString();
                }

                if (root.TryGetProperty("score_logic", out var slProp)) scoreLogic = slProp.GetInt32();
                if (root.TryGetProperty("score_tech", out var stProp)) scoreTech = stProp.GetInt32();
                if (root.TryGetProperty("score_communication", out var scProp)) scoreCommunication = scProp.GetInt32();
                if (root.TryGetProperty("next_question", out var nqProp)) nextQuestion = nqProp.GetString() ?? nextQuestion;

                // Đồng bộ hóa scoreTech và scoreCommunication bằng trung bình của rubric nhân 20 nếu có rubric_evaluation
                if (root.TryGetProperty("rubric_evaluation", out var rubObj))
                {
                    if (rubObj.TryGetProperty("technical_score", out var techScoreObj) && 
                        techScoreObj.TryGetProperty("average", out var techAvgEl))
                    {
                        if (techAvgEl.ValueKind == JsonValueKind.Number)
                        {
                            scoreTech = (int)Math.Round(techAvgEl.GetDouble() * 20);
                        }
                    }
                    if (rubObj.TryGetProperty("soft_skill_score", out var softScoreObj) && 
                        softScoreObj.TryGetProperty("average", out var softAvgEl))
                    {
                        if (softAvgEl.ValueKind == JsonValueKind.Number)
                        {
                            scoreCommunication = (int)Math.Round(softAvgEl.GetDouble() * 20);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to parse AI response JSON in SubmitReplyAsync: {ex.Message}");
                
                // Extract preamble as human-readable feedback
                var (_, preamble) = ExtractJsonAndPreamble(responseText);
                var cleanFeedback = !string.IsNullOrWhiteSpace(preamble) ? preamble : (responseText ?? "Cảm ơn câu trả lời của bạn.");
                
                // Remove raw JSON substring from the feedback if it got included
                if (cleanFeedback.Contains("{") && cleanFeedback.Contains("}"))
                {
                    int braceIndex = cleanFeedback.IndexOf("{");
                    if (braceIndex >= 0)
                    {
                        cleanFeedback = cleanFeedback.Substring(0, braceIndex).Trim();
                    }
                }
                if (string.IsNullOrWhiteSpace(cleanFeedback))
                {
                    cleanFeedback = "Cảm ơn câu trả lời của bạn.";
                }

                // Construct a valid JSON string for rubricJsonStr so the frontend parser succeeds
                var fallbackRubric = new
                {
                    question_type = "technical",
                    general_feedback = cleanFeedback,
                    strengths = new string[0],
                    improvements = new string[0]
                };
                
                rubricJsonStr = JsonSerializer.Serialize(fallbackRubric);
                nextQuestion = "Bạn vui lòng chia sẻ thêm hoặc chúng ta đi tiếp nhé.";
            }

            // Save evaluation into active turn
            activeTurn.CandidateTranscript = dto.Message;
            activeTurn.AiFeedback = string.IsNullOrWhiteSpace(rubricJsonStr) ? feedback : rubricJsonStr;
            activeTurn.ScoreLogic = scoreLogic;
            activeTurn.ScoreTech = scoreTech;
            activeTurn.ScoreCommunication = scoreCommunication;
            await _answerRepository.UpdateAsync(activeTurn);

            if (questionIndex >= totalQuestions)
            {
                // Tự động kết thúc session phỏng vấn
                session.Status = InterviewSessionStatus.COMPLETED;
                session.EndedAt = DateTime.UtcNow;
                await _sessionRepository.UpdateAsync(session);
                await _sessionRepository.SaveChangesAsync();

                // Warm-up cache: generate report on completion
                try
                {
                    await GenerateSessionReportAsync(sessionId, candidateId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Failed to pre-generate report in SubmitReplyAsync: {ex.Message}");
                }

                return new InterviewAnswerDto
                {
                    Id = Guid.Empty,
                    SessionId = sessionId,
                    QuestionText = nextQuestion,
                    CreatedAt = DateTime.UtcNow
                };
            }

            // Create next turn
            var nextTurn = new InterviewAnswers
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                QuestionText = nextQuestion,
                CreatedAt = DateTime.UtcNow
            };

            await _answerRepository.AddAsync(nextTurn);
            await _answerRepository.SaveChangesAsync();

            return new InterviewAnswerDto
            {
                Id = nextTurn.Id,
                SessionId = nextTurn.SessionId,
                QuestionText = nextTurn.QuestionText,
                CreatedAt = nextTurn.CreatedAt
            };
        }

        public async Task SwitchModelAsync(Guid sessionId, Guid candidateId, SwitchModelDto dto)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Session not found or unauthorized.");
            }

            session.AiProvider = dto.AiProvider;
            await _sessionRepository.UpdateAsync(session);
            await _sessionRepository.SaveChangesAsync();
        }

        public async Task CompleteSessionAsync(Guid sessionId, Guid candidateId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Session not found or unauthorized.");
            }

            session.Status = InterviewSessionStatus.COMPLETED;
            session.EndedAt = DateTime.UtcNow;
            await _sessionRepository.UpdateAsync(session);
            await _sessionRepository.SaveChangesAsync();

            // Warm-up cache: generate report on completion
            try
            {
                await GenerateSessionReportAsync(sessionId, candidateId);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[WARNING] Failed to pre-generate report in CompleteSessionAsync: {ex.Message}");
            }
        }

        public async Task DeleteSessionAsync(Guid sessionId, Guid candidateId)
        {
            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.CandidateId != candidateId)
            {
                throw new KeyNotFoundException("Session not found or unauthorized.");
            }

            var answers = await _answerRepository.GetBySessionIdAsync(sessionId);
            if (answers.Any())
            {
                await _answerRepository.DeleteRangeAsync(answers);
            }

            await _sessionRepository.DeleteAsync(session);
            await _sessionRepository.SaveChangesAsync();
        }

        private string DetermineRole(string jobTitle, string cvFileName, string cvText)
        {
            // 1. Prioritize Job Title from JD
            if (!string.IsNullOrWhiteSpace(jobTitle))
            {
                string jt = jobTitle.ToLower();
                if (jt.Contains("tester") || jt.Contains("test") || 
                    jt.Contains("qa") || jt.Contains("qc") || 
                    jt.Contains("automation"))
                {
                    return "Test";
                }
                if (jt.Contains("business analyst") || jt.Contains("product owner") || 
                    jt.Contains("scrum") || jt.Contains("analyst") ||
                    System.Text.RegularExpressions.Regex.IsMatch(jt, @"\bba\b"))
                {
                    return "BA";
                }
                return "Dev"; // Default if job title is specified but doesn't match Test/BA (e.g., "Developer", "Lập trình viên")
            }

            // 2. Fallback to CV if Job Title is not available
            string textToSearch = $"{cvFileName} {cvText}".ToLower();
            
            // To classify as Test, look for tester-specific terms first.
            // Avoid matching general "test" if it is just "unit test", "api test" etc.
            if (textToSearch.Contains("tester") || textToSearch.Contains("qa ") || 
                textToSearch.Contains("qc ") || textToSearch.Contains("automation test") || 
                textToSearch.Contains("manual test") || textToSearch.Contains("software testing"))
            {
                return "Test";
            }

            // To classify as BA, avoid matching Vietnamese "ba " (three)
            if (textToSearch.Contains("business analyst") || 
                textToSearch.Contains("product owner") || 
                textToSearch.Contains("product analyst") ||
                textToSearch.Contains("ba (business analyst)") ||
                textToSearch.Contains("system analyst"))
            {
                return "BA";
            }

            return "Dev"; // Default
        }

        private async Task<List<string>> GetSampleQuestionsAsync(string role, string exactLevel)
        {
            // Map exact level string to DB strings
            string dbLevel = exactLevel.ToLower() switch
            {
                var s when s.Contains("intern") || s.Contains("fresher") => "INTERN_FRESHER",
                var s when s.Contains("junior") => "JUNIOR",
                var s when s.Contains("middle") => "MIDDLE",
                var s when s.Contains("senior") => "SENIOR",
                _ => "JUNIOR"
            };

            // Query database for matching questions
            var questions = await _context.InterviewQuestionBank
                .Where(x => x.Industry.ToLower() == role.ToLower() && x.Level.ToUpper() == dbLevel)
                .Select(x => x.QuestionText)
                .ToListAsync();

            // If we don't have enough questions or no questions, fall back to InterviewRubricHelper static questions as backup
            if (questions.Count == 0)
            {
                var sampleQuestions = new List<string>();
                if (InterviewRubricHelper.RubricQuestions.TryGetValue(role, out var levelDict))
                {
                    if (dbLevel == "INTERN_FRESHER" && levelDict.TryGetValue("Intern/Fresher", out var qList1))
                    {
                        sampleQuestions.AddRange(qList1);
                    }
                    else if (dbLevel == "JUNIOR" && levelDict.TryGetValue("Junior", out var qList2))
                    {
                        sampleQuestions.AddRange(qList2);
                    }
                    else if (dbLevel == "MIDDLE" && levelDict.TryGetValue("Middle", out var qList3))
                    {
                        sampleQuestions.AddRange(qList3);
                    }
                    else if (dbLevel == "SENIOR" && levelDict.TryGetValue("Senior", out var qList4))
                    {
                        sampleQuestions.AddRange(qList4);
                    }
                }
                return sampleQuestions;
            }

            return questions;
        }

        private (string cleanJson, string preamble) ExtractJsonAndPreamble(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return (string.Empty, string.Empty);

            string cleanJson = text;
            string preamble = string.Empty;

            var match = System.Text.RegularExpressions.Regex.Match(text, @"```(?:json)?\s*([\s\S]*?)```");
            if (match.Success)
            {
                cleanJson = match.Groups[1].Value;
                int mdIndex = text.IndexOf("```");
                if (mdIndex > 0)
                {
                    preamble = text.Substring(0, mdIndex).Trim();
                }
            }

            var startIndex = cleanJson.IndexOf('{');
            var endIndex = cleanJson.LastIndexOf('}');
            if (startIndex >= 0 && endIndex >= startIndex)
            {
                if (startIndex > 0)
                {
                    string extraPreamble = cleanJson.Substring(0, startIndex).Trim();
                    preamble = string.IsNullOrWhiteSpace(preamble)
                        ? extraPreamble
                        : $"{preamble}\n\n{extraPreamble}";
                }
                cleanJson = cleanJson.Substring(startIndex, endIndex - startIndex + 1).Trim();
            }
            else
            {
                cleanJson = cleanJson.Trim();
            }

            return (cleanJson, preamble.Trim());
        }

        private async Task<InterviewReports?> GenerateSessionReportAsync(Guid sessionId, Guid candidateId)
        {
            // Check if report already exists
            var existingReport = await _context.InterviewReports
                .FirstOrDefaultAsync(r => r.SessionId == sessionId);
            if (existingReport != null)
            {
                return existingReport;
            }

            var session = await _sessionRepository.GetByIdAsync(sessionId);
            if (session == null || session.CandidateId != candidateId)
            {
                return null;
            }

            // Retrieve all answers for the session
            var answers = await _answerRepository.GetBySessionIdAsync(sessionId);
            if (answers == null || !answers.Any())
            {
                return null;
            }

            var validAnswers = answers.Where(a => a.CandidateTranscript != null).ToList();

            // Calculate Metrics using C#
            List<double> techScores = new List<double>();
            List<double> softScores = new List<double>();

            foreach (var a in validAnswers)
            {
                if (!string.IsNullOrWhiteSpace(a.AiFeedback))
                {
                    try
                    {
                        var jsonNode = JsonNode.Parse(a.AiFeedback);
                        if (jsonNode != null)
                        {
                            var techAvg = jsonNode["technical_score"]?["average"]?.GetValue<double>();
                            if (techAvg.HasValue) techScores.Add(techAvg.Value);

                            var softAvg = jsonNode["soft_skill_score"]?["average"]?.GetValue<double>();
                            if (softAvg.HasValue) softScores.Add(softAvg.Value);
                        }
                    }
                    catch { }
                }
            }

            double techAvgFinal = techScores.Any() ? Math.Round(techScores.Average(), 2) : 0;
            double softAvgFinal = softScores.Any() ? Math.Round(softScores.Average(), 2) : 0;

            double techStdDev = 0;
            if (techScores.Count > 1)
            {
                double sumOfSquares = techScores.Select(val => (val - techAvgFinal) * (val - techAvgFinal)).Sum();
                techStdDev = Math.Round(Math.Sqrt(sumOfSquares / techScores.Count), 2);
            }

            double softStdDev = 0;
            if (softScores.Count > 1)
            {
                double sumOfSquares = softScores.Select(val => (val - softAvgFinal) * (val - softAvgFinal)).Sum();
                softStdDev = Math.Round(Math.Sqrt(sumOfSquares / softScores.Count), 2);
            }

            int questionsTouched = validAnswers.Count;

            // Compute totalScore equivalent (for DB field)
            decimal totalScore = (decimal)Math.Round(((techAvgFinal + softAvgFinal) / 2.0) * 20.0);
            if (totalScore == 0 && validAnswers.Any())
            {
                var fallbackAvg = validAnswers.Average(a => ((a.ScoreLogic ?? 0) + (a.ScoreTech ?? 0) + (a.ScoreCommunication ?? 0)) / 3.0);
                totalScore = (decimal)Math.Round(fallbackAvg);
            }

            // Construct prompt for overall evaluation
            var systemPrompt = $"Bạn là một chuyên gia đánh giá nhân sự cao cấp. Nhiệm vụ của bạn là tổng hợp và đưa ra báo cáo đánh giá tổng quan cho buổi phỏng vấn thử (mock interview) của ứng viên.\n" +
                               "Bạn sẽ nhận được danh sách các câu hỏi của AI và câu trả lời của ứng viên, kèm theo điểm số và nhận xét từng câu.\n\n" +
                               "Dựa vào chi tiết lịch sử phỏng vấn, hãy đưa ra đánh giá tổng thể gồm:\n" +
                               "1. Mô hình lỗi lặp lại (pattern): Phát hiện thói quen hoặc lỗi ứng viên lặp lại nhiều lần (nếu có).\n" +
                               "2. Gợi ý hành động (action_items): 2-3 việc cụ thể cần làm tiếp theo.\n" +
                               "3. Đánh giá tổng quan (overall_feedback): Tóm tắt ngắn gọn và chuyên nghiệp về năng lực của ứng viên.\n" +
                               "4. Điểm mạnh nổi bật (strengths): Top 3 điểm mạnh nhất.\n" +
                               "5. Điểm cần cải thiện (improvements): Top 3 điểm cần cải thiện ưu tiên.\n\n" +
                               "Bạn BẮT BUỘC phải trả về kết quả theo định dạng JSON duy nhất như sau:\n" +
                               "{\n" +
                               "  \"pattern\": \"Ứng viên hay trả lời thiếu ví dụ thực tế trong các câu hỏi System Design...\",\n" +
                               "  \"strengths\": [\"Điểm mạnh 1\", \"Điểm mạnh 2\", \"Điểm mạnh 3\"],\n" +
                               "  \"improvements\": [\"Điểm cải thiện 1\", \"Điểm cải thiện 2\", \"Điểm cải thiện 3\"],\n" +
                               "  \"action_items\": [\"Hành động 1\", \"Hành động 2\"],\n" +
                               "  \"overall_feedback\": \"Đánh giá tổng quan...\"\n" +
                               "}\n\n" +
                               "Lưu ý: Chỉ trả về JSON thuần túy, không bao bọc trong khối code markdown hay bất kỳ văn bản nào ngoài JSON.";

            var turnsDescription = string.Join("\n\n", answers.Select((a, idx) => 
                $"LƯỢT HỎI {idx + 1}:\n" +
                $"AI Question: {a.QuestionText}\n" +
                $"Candidate Answer: {a.CandidateTranscript ?? "(Không trả lời)"}\n" +
                $"Scores: Logic={a.ScoreLogic}%, Tech={a.ScoreTech}%, Comm={a.ScoreCommunication}%\n" +
                $"Feedback: {a.AiFeedback}"));

            var responseText = await _aiService.GenerateTextAsync(
                prompt: $"Dưới đây là chi tiết buổi phỏng vấn:\n\n{turnsDescription}",
                systemPrompt: systemPrompt,
                providerName: session.AiProvider
            );

            // Clean & Parse response
            var (cleanJson, _) = ExtractJsonAndPreamble(responseText);
            string overallFeedbackJson = string.Empty;

            try
            {
                var jsonNode = JsonNode.Parse(cleanJson);
                if (jsonNode != null)
                {
                    overallFeedbackJson = jsonNode.ToJsonString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to parse AI overall report JSON: {ex.Message}");

                // Construct fallback overall feedback JSON
                var fallbackFeedback = new
                {
                    pattern = "",
                    strengths = new string[0],
                    improvements = new string[0],
                    action_items = new string[0],
                    overall_feedback = responseText ?? "Đã hoàn thành buổi phỏng vấn thử."
                };
                overallFeedbackJson = JsonSerializer.Serialize(fallbackFeedback);
            }

            var report = new InterviewReports
            {
                Id = Guid.NewGuid(),
                SessionId = sessionId,
                TotalScore = totalScore,
                OverallFeedback = overallFeedbackJson
            };

            _context.InterviewReports.Add(report);
            await _context.SaveChangesAsync();

            return report;
        }
    }
}
