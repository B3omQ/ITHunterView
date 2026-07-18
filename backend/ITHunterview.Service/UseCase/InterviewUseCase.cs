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
                    AiProvider = session.AiProvider
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
                AiProvider = session.AiProvider
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

            var session = new InterviewSessions
            {
                Id = Guid.NewGuid(),
                CandidateId = candidateId,
                JobId = dto.JobId,
                CvId = dto.CvId,
                DifficultyLevel = dto.DifficultyLevel,
                Status = InterviewSessionStatus.IN_PROGRESS,
                StartedAt = DateTime.UtcNow,
                AiProvider = provider
            };

            await _sessionRepository.AddAsync(session);
            await _sessionRepository.SaveChangesAsync();

            // Fetch context CV / Job details to inject in prompt
            string cvContext = "Chưa có thông tin CV.";
            string cvFileName = "";
            if (dto.CvId.HasValue)
            {
                var cv = await _cvRepository.GetByIdAsync(dto.CvId.Value);
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

            string jobContext = "Chưa có thông tin công việc (JD).";
            string jobTitle = "";
            if (dto.JobId.HasValue)
            {
                var job = await _jobPostingRepository.GetByIdAsync(dto.JobId.Value);
                if (job != null)
                {
                    jobTitle = job.Title ?? "";
                    jobContext = $"Title: {job.Title}\nDescription: {job.Description}\nRequirements: {job.Requirements}";
                }
            }

            // Phân loại Role & Seniority và trích xuất câu hỏi mẫu từ Rubric
            string role = DetermineRole(jobTitle, cvFileName, cvContext);
            var sampleQuestions = GetSampleQuestions(role, dto.DifficultyLevel);
            string rubricContext = sampleQuestions.Count > 0
                ? "Dưới đây là một số câu hỏi mẫu từ bộ quy chuẩn đánh giá của ITHunterView để bạn tham khảo phong cách, độ khó và nội dung:\n- " + string.Join("\n- ", sampleQuestions)
                : "";

            // Prompt chào hỏi và câu hỏi đầu tiên (Skills #1)
            // var variables = new Dictionary<string, string>
            // {
            //     { "DIFFICULTY_LEVEL", dto.DifficultyLevel.ToString() },
            //     { "ROLE", role },
            //     { "CV_TEXT", cvContext },
            //     { "JD_TEXT", jobContext },
            //     { "RUBRIC_CONTEXT", rubricContext }
            // };

            // var systemPrompt = await _promptManagementService.GetActivePromptContentWithVariablesAsync("MOCK_INTERVIEW_START", variables);
            
            // if (string.IsNullOrWhiteSpace(systemPrompt))
            // {
            //     throw new Exception("Active Prompt for MOCK_INTERVIEW_START not found.");
            // }
             var systemPrompt = $"Bạn là một người phỏng vấn IT tuyển dụng chuyên nghiệp. Nhiệm vụ của bạn là thực hiện một buổi phỏng vấn thử (mock interview) gồm đúng 6 câu hỏi (Role: {role}).\n\n" +
                               $"LỘ TRÌNH PHỎNG VẤN:\n" +
                               $"1. Câu 1 & 2: Kỹ năng chuyên môn / Soft skills (Skills)\n" +
                               $"2. Câu 3 & 4: Kinh nghiệm thực tế / Dự án (Experience)\n" +
                               $"3. Câu 5 & 6: Tình huống thực tế / Mức độ phù hợp với JD (JD & CV Match)\n\n" +
                               $"THÔNG TIN BỐ CẢNH:\n" +
                               $"--- START CV ---\n{cvContext}\n--- END CV ---\n\n" +
                               $"--- START JD ---\n{jobContext}\n--- END JD ---\n\n" +
                               $"{rubricContext}\n\n" +
                               $"LƯU Ý QUAN TRỌNG VỀ TÌNH HUỐNG LỆCH CÔNG NGHỆ:\n" +
                               $"- Hãy đối chiếu kỹ CV và JD. Nếu có sự lệch công nghệ lớn (ví dụ: JD yêu cầu .NET nhưng CV chỉ có Java), bạn PHẢI nhận biết được điều này và chuẩn bị các câu hỏi tình huống thích ứng công nghệ mới ở các câu tiếp theo.\n\n" +
                               $"YÊU CẦU CHO CÂU HỎI 1:\n" +
                               $"- Đây là câu hỏi số 1/6 (Chủ đề: Kỹ năng chuyên môn / Soft skills).\n" +
                               $"- Hãy đưa ra lời chào đón ứng viên thân thiện từ hệ thống ITHunterView, sau đó đặt câu hỏi đầu tiên về Kỹ năng chuyên môn hoặc Kỹ năng mềm phù hợp.\n" +
                               $"- Chỉ hỏi DUY NHẤT một câu hỏi chính trong mỗi lượt chat.\n" +
                               $"- Trả lời ngắn gọn bằng tiếng Việt.";

            var firstQuestion = await _aiService.GenerateTextAsync(
                prompt: "Bắt đầu buổi phỏng vấn thử.",
                systemPrompt: systemPrompt,
                providerName: provider
            );

            var firstTurn = new InterviewAnswers
            {
                Id = Guid.NewGuid(),
                SessionId = session.Id,
                QuestionText = firstQuestion ?? "Xin chào! Chúng ta hãy bắt đầu buổi phỏng vấn. Bạn hãy giới thiệu bản thân nhé.",
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
                AiProvider = session.AiProvider
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

            // Update candidate reply
            activeTurn.CandidateTranscript = dto.Message;
            await _answerRepository.UpdateAsync(activeTurn);

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
            if (session.JobId.HasValue)
            {
                var job = await _jobPostingRepository.GetByIdAsync(session.JobId.Value);
                if (job != null)
                {
                    jobTitle = job.Title ?? "";
                    jobContext = $"Title: {job.Title}\nDescription: {job.Description}\nRequirements: {job.Requirements}";
                }
            }

            // Phân loại Role & Seniority và trích xuất câu hỏi mẫu từ Rubric
            string role = DetermineRole(jobTitle, cvFileName, cvContext);
            var sampleQuestions = GetSampleQuestions(role, session.DifficultyLevel);
            string rubricContext = sampleQuestions.Count > 0
                ? "Dưới đây là một số câu hỏi mẫu từ bộ quy chuẩn đánh giá của ITHunterView để bạn tham khảo phong cách, độ khó và nội dung:\n- " + string.Join("\n- ", sampleQuestions)
                : "";

            // Fetch previous turns for context
            var history = await _answerRepository.GetBySessionIdAsync(sessionId);
            int questionIndex = history.Count; // Số câu hỏi đã được hỏi & trả lời (tính cả câu vừa trả lời)

            // Build conversation history
            var historyText = string.Join("\n\n", history.Select(h => 
                $"AI Question: {h.QuestionText}\nCandidate Answer: {h.CandidateTranscript ?? "(Chưa trả lời)"}"));

            // Định nghĩa hướng dẫn động cho từng câu hỏi tiếp theo
            string questionInstruction = "QUY TẮC QUAN TRỌNG: Mọi câu hỏi bạn đặt ra BẮT BUỘC phải dựa trên bối cảnh thực tế từ CV của ứng viên hoặc yêu cầu của JD. TUYỆT ĐỐI KHÔNG hỏi các câu lý thuyết chung chung như trong sách giáo khoa nếu không liên kết với một kỹ năng/dự án trong CV.\n\n";
            if (questionIndex == 1) // Cần sinh Q2
            {
                questionInstruction += "ĐÂY LÀ LƯỢT HỎI SỐ 2/6 (Chủ đề: Kỹ năng chuyên môn / Soft skills).\n" +
                                       "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                       "- Dựa vào một kỹ năng hoặc công cụ cụ thể được liệt kê trong CV, hãy đặt câu hỏi tiếp theo (Q2) để kiểm tra độ sâu chuyên môn của họ.";
            }
            else if (questionIndex == 2) // Cần sinh Q3
            {
                questionInstruction += "ĐÂY LÀ LƯỢT HỎI SỐ 3/6 (Chủ đề: Kinh nghiệm thực tế / Dự án).\n" +
                                       "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                       "- Hãy chọn một DỰ ÁN cụ thể trong CV của ứng viên và đặt câu hỏi (Q3) khai thác sâu về vai trò của họ, thách thức kỹ thuật lớn nhất họ gặp phải hoặc cách họ giải quyết vấn đề.";
            }
            else if (questionIndex == 3) // Cần sinh Q4
            {
                questionInstruction += "ĐÂY LÀ LƯỢT HỎI SỐ 4/6 (Chủ đề: Kinh nghiệm thực tế / System / Quy trình).\n" +
                                       "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                       "- Dựa vào các framework/hệ thống mà ứng viên đã làm, đặt câu hỏi (Q4) về cách họ tối ưu hiệu năng, clean code hoặc thiết kế hệ thống.";
            }
            else if (questionIndex == 4) // Cần sinh Q5
            {
                questionInstruction += "ĐÂY LÀ LƯỢT HỎI SỐ 5/6 (Chủ đề: Tình huống / Mức độ phù hợp với JD).\n" +
                                       "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                       "- Hãy đối chiếu CV của ứng viên với các yêu cầu của JD. Đặt câu hỏi tình huống thực tế (Q5) để kiểm tra xem họ có thể đáp ứng được một yêu cầu khó/đặc thù trong JD hay không.";
            }
            else if (questionIndex == 5) // Cần sinh Q6
            {
                questionInstruction += "ĐÂY LÀ LƯỢT HỎI SỐ 6/6 (Chủ đề: Câu hỏi kết thúc / Giao tiếp).\n" +
                                       "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                       "- Đặt câu hỏi tình huống hoặc kỹ năng mềm cuối cùng (Q6) liên quan mật thiết đến vị trí ứng tuyển để hoàn tất buổi phỏng vấn.";
            }
            else // questionIndex >= 6, đã trả lời xong câu số 6
            {
                questionInstruction = "ĐÂY LÀ LƯỢT ĐÁNH GIÁ CUỐI CÙNG (Buổi phỏng vấn kết thúc).\n" +
                                      "- Ứng viên đã hoàn thành toàn bộ 6 câu hỏi.\n" +
                                      "- Nhận xét chi tiết và mang tính xây dựng tổng quát cho toàn bộ buổi phỏng vấn (ở trường 'feedback').\n" +
                                      "- Ở trường 'next_question', hãy trả về câu chào tạm biệt lịch sự từ ITHunterView và thông báo rằng buổi phỏng vấn thử đã kết thúc thành công.";
            }

            // var variables = new Dictionary<string, string>
            // {
            //     { "DIFFICULTY_LEVEL", session.DifficultyLevel.ToString() },
            //     { "ROLE", role },
            //     { "CV_TEXT", cvContext },
            //     { "JD_TEXT", jobContext },
            //     { "RUBRIC_CONTEXT", rubricContext },
            //     { "QUESTION_INSTRUCTION", questionInstruction }
            // };

            // var systemPrompt = await _promptManagementService.GetActivePromptContentWithVariablesAsync("MOCK_INTERVIEW_NEXT", variables);

            // if (string.IsNullOrWhiteSpace(systemPrompt))
            // {
            //     throw new Exception("Active Prompt for MOCK_INTERVIEW_NEXT not found.");
            // }
            var systemPrompt = $"Bạn là một người phỏng vấn IT tuyển dụng chuyên nghiệp. Bạn đang thực hiện một buổi phỏng vấn thử với ứng viên (Role: {role}).\n\n" +
                               $"THÔNG TIN BỐ CẢNH:\n" +
                               $"--- START CV ---\n{cvContext}\n--- END CV ---\n\n" +
                               $"--- START JD ---\n{jobContext}\n--- END JD ---\n\n" +
                               $"{rubricContext}\n\n" +
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

                // Attempt to mutate the JSON and prepend the preamble to general_feedback
                if (!string.IsNullOrWhiteSpace(cleanJson))
                {
                    try
                    {
                        var jsonNode = JsonNode.Parse(cleanJson);
                        if (jsonNode != null && !string.IsNullOrWhiteSpace(preamble))
                        {
                            var rubricNode = jsonNode["rubric_evaluation"];
                            if (rubricNode != null)
                            {
                                var generalFeedback = rubricNode["general_feedback"]?.GetValue<string>();
                                string combinedFeedback = string.IsNullOrWhiteSpace(generalFeedback)
                                    ? preamble
                                    : $"{preamble}\n\n{generalFeedback}";
                                rubricNode["general_feedback"] = combinedFeedback;
                                cleanJson = jsonNode.ToJsonString();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARNING] Failed to parse or mutate JSON nodes in SubmitReplyAsync: {ex.Message}");
                    }
                }

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
            activeTurn.AiFeedback = string.IsNullOrWhiteSpace(rubricJsonStr) ? feedback : rubricJsonStr;
            activeTurn.ScoreLogic = scoreLogic;
            activeTurn.ScoreTech = scoreTech;
            activeTurn.ScoreCommunication = scoreCommunication;
            await _answerRepository.UpdateAsync(activeTurn);

            if (questionIndex >= 6)
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
            string textToSearch = $"{jobTitle} {cvFileName} {cvText}".ToLower();

            if (textToSearch.Contains("tester") || textToSearch.Contains("test") || 
                textToSearch.Contains("qa") || textToSearch.Contains("qc") || 
                textToSearch.Contains("automation"))
            {
                return "Test";
            }
            else if (textToSearch.Contains("ba ") || textToSearch.Contains("business analyst") || 
                     textToSearch.Contains("product owner") || textToSearch.Contains(" scm ") || 
                     textToSearch.Contains("scrum") || textToSearch.Contains(" product analyst "))
            {
                return "BA";
            }

            return "Dev"; // Default
        }

        private List<string> GetSampleQuestions(string role, DifficultyLevel difficulty)
        {
            var sampleQuestions = new List<string>();
            if (InterviewRubricHelper.RubricQuestions.TryGetValue(role, out var levelDict))
            {
                if (difficulty == DifficultyLevel.EASY && levelDict.TryGetValue("Intern/Fresher", out var qList1))
                {
                    sampleQuestions.AddRange(qList1);
                }
                else if (difficulty == DifficultyLevel.MEDIUM)
                {
                    if (levelDict.TryGetValue("Junior", out var qList2)) sampleQuestions.AddRange(qList2);
                    if (levelDict.TryGetValue("Middle", out var qList3)) sampleQuestions.AddRange(qList3);
                }
                else if (difficulty == DifficultyLevel.HARD && levelDict.TryGetValue("Senior", out var qList4))
                {
                    sampleQuestions.AddRange(qList4);
                }
            }
            return sampleQuestions;
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
                               $"Hệ thống đã tự động tính toán các chỉ số trung bình (thang điểm 1-5):\n" +
                               $"- Điểm Technical trung bình: {techAvgFinal}/5 (Độ lệch chuẩn: {techStdDev})\n" +
                               $"- Điểm Soft Skills trung bình: {softAvgFinal}/5 (Độ lệch chuẩn: {softStdDev})\n" +
                               $"- Số câu đã trả lời: {questionsTouched}\n\n" +
                               "Dựa vào dữ liệu trên và chi tiết lịch sử phỏng vấn, hãy đưa ra đánh giá tổng thể gồm:\n" +
                               "1. Mức độ sẵn sàng (readiness_level): Phân loại ứng viên vào 1 trong các mức (Chưa sẵn sàng, Cần luyện thêm, Sẵn sàng ở mức junior, Sẵn sàng ở mức mid, Sẵn sàng phỏng vấn thật).\n" +
                               "2. Mô hình lỗi lặp lại (pattern): Phát hiện thói quen hoặc lỗi ứng viên lặp lại nhiều lần (nếu có).\n" +
                               "3. Gợi ý hành động (action_items): 2-3 việc cụ thể cần làm tiếp theo.\n" +
                               "4. Đánh giá tổng quan (overall_feedback): Tóm tắt ngắn gọn và chuyên nghiệp về năng lực của ứng viên.\n" +
                               "5. Điểm mạnh nổi bật (strengths): Top 3 điểm mạnh nhất.\n" +
                               "6. Điểm cần cải thiện (improvements): Top 3 điểm cần cải thiện ưu tiên.\n\n" +
                               "Bạn BẮT BUỘC phải trả về kết quả theo định dạng JSON duy nhất như sau:\n" +
                               "{\n" +
                               "  \"readiness_level\": \"Sẵn sàng ở mức mid\",\n" +
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
                    var metricsNode = new JsonObject
                    {
                        ["technical_avg"] = techAvgFinal,
                        ["soft_skills_avg"] = softAvgFinal,
                        ["technical_stddev"] = techStdDev,
                        ["soft_skills_stddev"] = softStdDev,
                        ["questions_touched"] = questionsTouched
                    };
                    jsonNode["metrics"] = metricsNode;
                    overallFeedbackJson = jsonNode.ToJsonString();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Failed to parse AI overall report JSON: {ex.Message}");

                // Construct fallback overall feedback JSON
                var fallbackFeedback = new
                {
                    metrics = new
                    {
                        technical_avg = techAvgFinal,
                        soft_skills_avg = softAvgFinal,
                        technical_stddev = techStdDev,
                        soft_skills_stddev = softStdDev,
                        questions_touched = questionsTouched
                    },
                    readiness_level = "Chưa đánh giá được",
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
