using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Interview;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.UseCase
{
    public class InterviewUseCase : IInterviewUseCase
    {
        private readonly IInterviewSessionRepository _sessionRepository;
        private readonly IInterviewAnswerRepository _answerRepository;
        private readonly ICvRepository _cvRepository;
        private readonly IJobPostingRepository _jobPostingRepository;
        private readonly IAiService _aiService;

        public InterviewUseCase(
            IInterviewSessionRepository sessionRepository,
            IInterviewAnswerRepository answerRepository,
            ICvRepository cvRepository,
            IJobPostingRepository jobPostingRepository,
            IAiService aiService)
        {
            _sessionRepository = sessionRepository;
            _answerRepository = answerRepository;
            _cvRepository = cvRepository;
            _jobPostingRepository = jobPostingRepository;
            _aiService = aiService;
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

            return new InterviewSessionDetailDto
            {
                Session = sessionDto,
                Messages = messages
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
                    if (!string.IsNullOrWhiteSpace(cv.ParsedData))
                    {
                        cvContext = cv.ParsedData;
                    }
                }
            }

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
            var systemPrompt = $"Bạn là một người phỏng vấn IT tuyển dụng chuyên nghiệp. Nhiệm vụ của bạn là thực hiện một buổi phỏng vấn thử (mock interview) gồm đúng 6 câu hỏi ở cấp độ {dto.DifficultyLevel} (Role: {role}).\n\n" +
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
                    if (!string.IsNullOrWhiteSpace(cv.ParsedData))
                    {
                        cvContext = cv.ParsedData;
                    }
                }
            }

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
            string questionInstruction = "";
            if (questionIndex == 1) // Cần sinh Q2
            {
                questionInstruction = "ĐÂY LÀ LƯỢT HỎI SỐ 2/6 (Chủ đề: Kỹ năng chuyên môn / Soft skills).\n" +
                                      "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                      "- Đặt câu hỏi tiếp theo (Q2) về Kỹ năng chuyên môn hoặc Kỹ năng mềm khác phù hợp.";
            }
            else if (questionIndex == 2) // Cần sinh Q3
            {
                questionInstruction = "ĐÂY LÀ LƯỢT HỎI SỐ 3/6 (Chủ đề: Kinh nghiệm thực tế / Dự án).\n" +
                                      "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                      "- Đặt câu hỏi tiếp theo (Q3) khai thác sâu hơn về dự án thực tế trong CV của họ hoặc cách họ xử lý khó khăn kỹ thuật.";
            }
            else if (questionIndex == 3) // Cần sinh Q4
            {
                questionInstruction = "ĐÂY LÀ LƯỢT HỎI SỐ 4/6 (Chủ đề: Kinh nghiệm thực tế / Dự án).\n" +
                                      "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                      "- Đặt câu hỏi tiếp theo (Q4) hỏi thêm một khía cạnh về quy trình làm việc, tối ưu hiệu năng, clean code hoặc thiết kế hệ thống.";
            }
            else if (questionIndex == 4) // Cần sinh Q5
            {
                questionInstruction = "ĐÂY LÀ LƯỢT HỎI SỐ 5/6 (Chủ đề: Tình huống / Mức độ phù hợp với JD).\n" +
                                      "- Hãy đối chiếu CV của ứng viên với các yêu cầu của JD. " +
                                      "Nếu có sự lệch công nghệ lớn (ví dụ: JD yêu cầu .NET nhưng CV chỉ có Java), bạn hãy đưa ra câu hỏi tình huống: \"Mặc dù CV của bạn chủ yếu là Java, nhưng vị trí này yêu cầu .NET, bạn sẽ tiếp cận/tự học như thế nào?\" hoặc tương tự. " +
                                      "Nếu không có lệch công nghệ lớn, hãy đặt câu hỏi tình huống thực tế khó để kiểm tra sự phù hợp của họ với các yêu cầu khác trong JD.\n" +
                                      "- Đặt câu hỏi tiếp theo (Q5) theo hướng dẫn trên.";
            }
            else if (questionIndex == 5) // Cần sinh Q6
            {
                questionInstruction = "ĐÂY LÀ LƯỢT HỎI SỐ 6/6 (Chủ đề: Tình huống / Mức độ phù hợp với JD).\n" +
                                      "- Bạn hãy nhận xét ngắn gọn câu trả lời vừa rồi của ứng viên (2-3 câu).\n" +
                                      "- Đặt câu hỏi tình huống cuối cùng (Q6) để hoàn tất buổi phỏng vấn.";
            }
            else // questionIndex >= 6, đã trả lời xong câu số 6
            {
                questionInstruction = "ĐÂY LÀ LƯỢT ĐÁNH GIÁ CUỐI CÙNG (Buổi phỏng vấn kết thúc).\n" +
                                      "- Ứng viên đã hoàn thành toàn bộ 6 câu hỏi.\n" +
                                      "- Nhận xét chi tiết và mang tính xây dựng tổng quát cho toàn bộ buổi phỏng vấn (ở trường 'feedback').\n" +
                                      "- Ở trường 'next_question', hãy trả về câu chào tạm biệt lịch sự từ ITHunterView và thông báo rằng buổi phỏng vấn thử đã kết thúc thành công.";
            }

            var systemPrompt = $"Bạn là một người phỏng vấn IT tuyển dụng chuyên nghiệp. Bạn đang thực hiện một buổi phỏng vấn thử với ứng viên ở cấp độ {session.DifficultyLevel} (Role: {role}).\n\n" +
                               $"THÔNG TIN BỐ CẢNH:\n" +
                               $"--- START CV ---\n{cvContext}\n--- END CV ---\n\n" +
                               $"--- START JD ---\n{jobContext}\n--- END JD ---\n\n" +
                               $"{rubricContext}\n\n" +
                               $"HƯỚNG DẪN LƯỢT NÀY:\n" +
                               $"{questionInstruction}\n\n" +
                               "Bạn BẮT BUỘC phải trả về kết quả theo định dạng JSON duy nhất như sau:\n" +
                               "{\n" +
                               "  \"feedback\": \"Nhận xét câu trả lời mới nhất (hoặc tổng kết nếu đã xong 6 câu)...\",\n" +
                               "  \"score_logic\": 80,\n" +
                               "  \"score_tech\": 85,\n" +
                               "  \"score_communication\": 90,\n" +
                               "  \"next_question\": \"Câu hỏi tiếp theo (hoặc lời tạm biệt kết thúc phỏng vấn)...\"\n" +
                               "}\n\n" +
                               "Lưu ý: Chỉ trả về JSON thuần túy, không bao bọc trong khối code markdown hay bất kỳ văn bản nào ngoài JSON.";

            var responseText = await _aiService.GenerateTextAsync(
                prompt: $"Lịch sử phỏng vấn:\n{historyText}\n\nỨng viên trả lời mới nhất: \"{dto.Message}\"",
                systemPrompt: systemPrompt,
                providerName: session.AiProvider
            );

            // Parse response
            var feedback = "Cảm ơn câu trả lời của bạn.";
            var scoreLogic = 70;
            var scoreTech = 70;
            var scoreCommunication = 70;
            var nextQuestion = "Bạn sẵn sàng cho câu hỏi tiếp theo chưa?";

            try
            {
                var cleanJson = responseText ?? "";
                if (cleanJson.Contains("```json"))
                {
                    cleanJson = cleanJson.Substring(cleanJson.IndexOf("```json") + 7);
                    if (cleanJson.Contains("```"))
                    {
                        cleanJson = cleanJson.Substring(0, cleanJson.IndexOf("```"));
                    }
                }
                else if (cleanJson.Contains("```"))
                {
                    cleanJson = cleanJson.Substring(cleanJson.IndexOf("```") + 3);
                    if (cleanJson.Contains("```"))
                    {
                        cleanJson = cleanJson.Substring(0, cleanJson.IndexOf("```"));
                    }
                }
                cleanJson = cleanJson.Trim();

                using var doc = JsonDocument.Parse(cleanJson);
                var root = doc.RootElement;

                if (root.TryGetProperty("feedback", out var fProp)) feedback = fProp.GetString() ?? feedback;
                if (root.TryGetProperty("score_logic", out var slProp)) scoreLogic = slProp.GetInt32();
                if (root.TryGetProperty("score_tech", out var stProp)) scoreTech = stProp.GetInt32();
                if (root.TryGetProperty("score_communication", out var scProp)) scoreCommunication = scProp.GetInt32();
                if (root.TryGetProperty("next_question", out var nqProp)) nextQuestion = nqProp.GetString() ?? nextQuestion;
            }
            catch
            {
                // Fallback if parsing fails
                feedback = responseText ?? feedback;
                nextQuestion = "Bạn vui lòng chia sẻ thêm hoặc chúng ta đi tiếp nhé.";
            }

            // Save evaluation into active turn
            activeTurn.AiFeedback = feedback;
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
    }
}
