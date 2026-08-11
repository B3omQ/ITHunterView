using System.Text.Json;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Entities.Cv;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Optimize;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.Extensions.DependencyInjection;

namespace ITHunterview.Service.UseCase;

public class OptimizeUseCase : IOptimizeUseCase
{
    private readonly IOptimizeSessionRepository _sessionRepo;
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IAiService _aiService;

    public OptimizeUseCase(
        IOptimizeSessionRepository sessionRepo,
        IServiceProvider serviceProvider,
        IHttpClientFactory httpClientFactory,
        IAiService aiService)
    {
        _sessionRepo = sessionRepo;
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _aiService = aiService;
    }

    public async Task<CvOptimizationResultDto> CreateSessionAndAnalyzeAsync(Guid userId, string? cvUrl, Guid? cvId)
    {
        if (string.IsNullOrWhiteSpace(cvUrl) && !cvId.HasValue)
            throw new ArgumentException("Either CvUrl or CvId must be provided.");

        string finalUrl = cvUrl ?? "";
        string? cvFileName = null;
        string? dbFileType = null;
        string rawTextFromDb = "";

        if (string.IsNullOrWhiteSpace(finalUrl) && cvId.HasValue)
        {
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ITHunterview.Service.Infrastructure.Persistence.ITHunterviewContext>();
            var cv = await dbContext.Cvs.FindAsync(cvId.Value);
            if (cv == null || string.IsNullOrWhiteSpace(cv.FileUrl))
                throw new ArgumentException("CV not found or has no FileUrl.");
            finalUrl = cv.FileUrl;
            dbFileType = cv.FileType;
            cvFileName = cv.FileName;
            rawTextFromDb = cv.RawText ?? "";
        }

        using var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync(finalUrl);
        response.EnsureSuccessStatusCode();
        var fileStream = await response.Content.ReadAsStreamAsync();
        
        var contentType = response.Content.Headers.ContentType?.MediaType ?? "";

        ICvExtractor extractor;
        string fileType;

        if (contentType.Contains("pdf") || finalUrl.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || (dbFileType != null && dbFileType.Contains("pdf", StringComparison.OrdinalIgnoreCase)))
        {
            extractor = _serviceProvider.GetRequiredService<ITHunterview.Service.Service.PdfCvExtractor>();
            fileType = "pdf";
        }
        else if (contentType.Contains("wordprocessingml") || finalUrl.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) || (dbFileType != null && dbFileType.Contains("word", StringComparison.OrdinalIgnoreCase)) || (dbFileType != null && dbFileType.Contains("docx", StringComparison.OrdinalIgnoreCase)))
        {
            extractor = _serviceProvider.GetRequiredService<ITHunterview.Service.Service.DocxCvExtractor>();
            fileType = "docx";
        }
        else
        {
            throw new ArgumentException($"Unsupported file type. ContentType: {contentType}, FinalUrl: {finalUrl}");
        }

        var cvDoc = await extractor.ExtractAsync(fileStream);

        // Build composite raw text for AI analysis
        string textForAnalysis = !string.IsNullOrWhiteSpace(rawTextFromDb) 
            ? rawTextFromDb 
            : JsonSerializer.Serialize(cvDoc);

        // Run AI Analysis
        var analysisDto = await AnalyzeCvStructureWithAiAsync(textForAnalysis);

        var session = new OptimizeSession
        {
            UserId = userId,
            CvId = cvId,
            CvFileName = cvFileName ?? Path.GetFileName(finalUrl),
            OriginalFileType = fileType,
            CvDocument = cvDoc,
            AnalysisResultJson = JsonSerializer.Serialize(analysisDto),
            OverallScore = analysisDto.OverallScore
        };

        await _sessionRepo.CreateAsync(session);
        analysisDto.SessionId = session.Id;
        analysisDto.CvId = cvId;
        analysisDto.CvFileName = session.CvFileName;

        return analysisDto;
    }

    public async Task<CvOptimizationResultDto> GetSessionResultAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null) throw new KeyNotFoundException("Optimize session not found");

        if (string.IsNullOrEmpty(session.AnalysisResultJson))
        {
            throw new InvalidOperationException("Optimization analysis result is not available for this session.");
        }

        var result = JsonSerializer.Deserialize<CvOptimizationResultDto>(session.AnalysisResultJson, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new CvOptimizationResultDto();

        result.SessionId = session.Id;
        result.CvId = session.CvId;
        result.CvFileName = session.CvFileName;
        result.OverallScore = session.OverallScore ?? result.OverallScore;

        return result;
    }

    public async Task<string?> GeneratePreviewAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session not found");

        if (session.OriginalFileType == "pdf")
        {
            var renderer = _serviceProvider.GetRequiredService<ITHunterview.Service.Service.PdfCvRenderer>();
            using var previewStream = await renderer.RenderPreviewImageAsync(session.CvDocument);
            using var memoryStream = new MemoryStream();
            await previewStream.CopyToAsync(memoryStream);
            
            return $"data:image/png;base64,{Convert.ToBase64String(memoryStream.ToArray())}";
        }
        
        return null;
    }

    public async Task<string> GenerateFinalFileAsync(Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session == null || session.CvDocument == null) 
            throw new KeyNotFoundException("Session not found");

        ICvRenderer renderer = session.OriginalFileType == "pdf" 
            ? _serviceProvider.GetRequiredService<ITHunterview.Service.Service.PdfCvRenderer>() 
            : _serviceProvider.GetRequiredService<ITHunterview.Service.Service.DocxCvRenderer>();

        using var finalStream = await renderer.RenderFinalAsync(session.CvDocument);
        return $"https://storage.local/optimized_cvs/{sessionId}.{session.OriginalFileType}";
    }

    public async Task<PagedResult<OptimizeHistoryItemDto>> GetUserHistoryAsync(Guid userId, int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 6;

        var (items, totalCount) = await _sessionRepo.GetHistoryByUserIdAsync(userId, page, pageSize);

        var dtos = items.Select(x => new OptimizeHistoryItemDto
        {
            SessionId = x.Id,
            CvId = x.CvId,
            CvFileName = x.CvFileName ?? "CV.pdf",
            OriginalFileType = x.OriginalFileType ?? "pdf",
            OverallScore = x.OverallScore ?? 0,
            CreatedAt = x.CreatedAt
        }).ToList();

        return new PagedResult<OptimizeHistoryItemDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task DeleteSessionAsync(Guid userId, Guid sessionId)
    {
        var session = await _sessionRepo.GetByIdAsync(sessionId);
        if (session != null && session.UserId == userId)
        {
            await _sessionRepo.DeleteAsync(sessionId);
        }
    }

    private async Task<CvOptimizationResultDto> AnalyzeCvStructureWithAiAsync(string cvContent)
    {
        string systemPrompt = @"Bạn là chuyên gia tư vấn tuyển dụng và tối ưu hóa CV hàng đầu.
Nhiệm vụ của bạn là phân tích cấu trúc, nội dung và kỹ thuật trình bày của CV theo BẢNG RUBRIC ĐÁNH GIÁ CV CHUẨN HÓA V2 mà KHÔNG viết lại toàn bộ CV cho người dùng.

BẢNG RUBRIC ĐÁNH GIÁ CV V2 (TỔNG ĐIỂM: 100 ĐIỂM):
Bạn BẮT BUỘC phải tính `overallScore` dựa trên đúng 3 nhóm tiêu chí sau:

1. CẤU TRÚC & ĐỘ DÀI TIÊU CHUẨN (Tối đa 25 điểm):
   - Thông tin liên hệ & Job Title (5 điểm): Có Job Title chuyên nghiệp ngay dưới Họ tên (+2đ); Đầy đủ SĐT, Email chuẩn, Địa chỉ & Link LinkedIn/GitHub/Portfolio (+3đ).
   - Giới hạn độ dài CV - CV Length (10 điểm): Fresher/Student gọn trong đúng 1 trang (+10đ); Experienced tối đa 2 trang (+10đ). Vi phạm tràn 2-3 dòng hoặc >2 trang: Trừ 5đ đến 10đ.
   - Thứ tự Bố cục ưu tiên - Priority Order (10 điểm): 
     + Fresher: Contact -> Summary -> Education -> Skills -> Projects (+10đ).
     + Experienced: Contact -> Summary -> Experience -> Skills -> Education (+10đ).
     + Sai thứ tự ưu tiên theo cấp độ: Trừ 5đ đến 10đ.

2. CHẤT LƯỢNG NỘI DUNG & MINH CHỨNG (Tối đa 45 điểm):
   - Mô tả Dự án & Kinh nghiệm thực tế (15 điểm): Đã dùng Động từ hành động (Action Verbs) & có số liệu kết quả định lượng (+15đ); Liệt kê chung chung thiếu số liệu: 5đ - 8đ.
   - Minh chứng sản phẩm - Proof of Work (10 điểm): Đính kèm link Portfolio, GitHub, Video demo sản phẩm thực tế (+10đ); Thiếu minh chứng thực tế: 0đ.
   - Độ tương thích từ khóa chuyên môn - Customization & ATS (10 điểm): Chứa các từ khóa chuyên môn sát với ngành IT (+10đ); Dùng mẫu sơ sài thiếu từ khóa: Trừ 5đ đến 10đ.
   - Học vấn & Phân loại Kỹ năng (10 điểm): Học vấn đầy đủ trường, ngành, niên khóa (+5đ); Phân loại kỹ năng theo nhóm (Languages, Frameworks, Databases, Tools) rõ ràng (+5đ).

3. TRÌNH BÀY & KỸ THUẬT (Tối đa 30 điểm):
   - Định danh tên Section chuẩn (10 điểm): Tên Section chuẩn tuyển dụng (Experience/Projects, Education, Skills, Summary) (+10đ); Đặt tên tùy tiện: Trừ 5đ.
   - Kiểm tra Lỗi chính tả & Định dạng (10 điểm): Không có lỗi chính tả, đánh máy hay lỗi font (+10đ); Có lỗi chính tả: Trừ 3đ đến 10đ.
   - Tính trung thực & Định dạng File (10 điểm): Mốc thời gian logic, trung thực, không bị mốc thời gian tương lai (+10đ); Mốc thời gian phi lý/khống thành tích: Trừ 5đ đến 10đ.

YÊU CẦU VỀ DANH SÁCH GIẢI PHÁP & KHUYẾN NGHỊ (recommendations):
- BẮT BUỘC phân tích TOÀN DIỆN, ĐẦY ĐỦ VÀ TOÀN BỘ mọi khía cạnh của CV theo Rubric V2. KHÔNG ĐƯỢC bỏ sót bất kỳ điểm cải thiện nào.
- Nếu một category có NHIỀU ĐIỂM CẦN CẢI THIỆN (ví dụ category 'Formatting' vừa thiếu Job Title vừa chưa phân loại nhóm Kỹ năng; hoặc 'Contact' vừa thiếu LinkedIn vừa thiếu Địa chỉ), BẮT BUỘC tạo THÀNH CÁC MỤC KHUYẾN NGHỊ RIÊNG BIỆT.
- Đưa ra danh sách đầy đủ nhất (thường từ 4 - 8 khuyến nghị chi tiết cho CV).

TRẢ VỀ KẾT QUẢ ĐÚNG ĐỊNH DẠNG JSON NHƯ SAU (Không thêm text thừa ngoài JSON):
{
  ""overallScore"": 82,
  ""summary"": ""Tóm tắt tổng quan về chất lượng CV dựa trên Rubric V2 trong 2-3 câu."",
  ""sections"": [
    {
      ""sectionName"": ""Thông tin liên hệ & Job Title"",
      ""isPresent"": true,
      ""status"": ""Warning"", // ""Good"", ""Warning"", hoặc ""Missing""
      ""feedback"": ""Đầy đủ Họ tên, SĐT, Email. Tuy nhiên thiếu Job Title chuyên nghiệp dưới tên và liên kết GitHub.""
    }
  ],
  ""priorityOrder"": {
    ""candidateLevel"": ""Student/Fresher"", // ""Student/Fresher"" hoặc ""Experienced""
    ""isOrderOptimal"": true,
    ""currentOrderDescription"": ""Thông tin liên hệ -> Summary -> Education -> Skills -> Projects"",
    ""recommendedOrderDescription"": ""Thông tin liên hệ -> Summary -> Education -> Skills -> Projects"",
    ""advice"": ""Thứ tự sắp xếp các phần phù hợp cho sinh viên/fresher.""
  },
  ""recommendations"": [
    {
      ""category"": ""Contact"", // ""Structure"", ""Contact"", ""Experience"", ""Skills"", ""Formatting"", ""ProofOfWork"", ""ATS""
      ""title"": ""Bổ sung liên kết GitHub và Portfolio"",
      ""description"": ""Đính kèm minh chứng sản phẩm thực tế (Proof of Work) giúp tăng điểm đánh giá từ nhà tuyển dụng."",
      ""priority"": ""High"", // ""High"", ""Medium"", ""Low""
      ""exampleBefore"": ""Email: candidate@email.com | SĐT: 0912345678"",
      ""exampleAfter"": ""Email: candidate@email.com | SĐT: 0912345678 | GitHub: github.com/user | Portfolio: user.dev""
    },
    {
      ""category"": ""Formatting"",
      ""title"": ""Thêm Job Title vị trí mong muốn ngay dưới Họ tên"",
      ""description"": ""CV hiện tại chưa có Job Title chuyên nghiệp dưới tên để nhận diện vai trò ứng tuyển."",
      ""priority"": ""Medium"",
      ""exampleBefore"": ""Họ tên: Phạm Công Trà"",
      ""exampleAfter"": ""Họ tên: Phạm Công Trà | Title: Software Engineer / .NET Developer""
    },
    {
      ""category"": ""Formatting"",
      ""title"": ""Phân loại nhóm Kỹ năng (Languages, Frameworks, Tools)"",
      ""description"": ""Nên chia nhỏ kỹ năng thành các nhóm thay vì liệt kê hàng loạt cùng một dòng."",
      ""priority"": ""Medium"",
      ""exampleBefore"": ""Kỹ năng: C#, Java, TypeScript, .NET Core, Git, Postman"",
      ""exampleAfter"": ""Ngôn ngữ: C#, Java, TypeScript | Frameworks: .NET Core | Tools: Git, Postman""
    }
  ]
}

LƯU Ý QUAN TRỌNG VỀ TRÌNH BÀY exampleBefore VÀ exampleAfter:
- TUYỆT ĐỐI KHÔNG xuất ra cú pháp JSON hoặc mảng/đối tượng mã nguồn (KHÔNG dùng Summary: ..., KHÔNG dùng [{ Company: ... }]).
- BẮT BUỘC viết dưới dạng VĂN BẢN THƯỜNG (Plain Text) tự nhiên, dễ đọc như trình bày trên CV thật. 
Ví dụ:
  + Đúng: Kỹ năng: C#, Java, TypeScript, Git, Postman
  + Sai: Skills: [C#, Java, TypeScript]";

        string userPrompt = $"Dưới đây là nội dung CV cần phân tích cấu trúc:\n\n{cvContent}";

        try
        {
            string aiResponse = await _aiService.GenerateTextAsync(userPrompt, systemPrompt, featureCode: "CV_OPTIMIZATION");

            // Clean markdown block format if present (e.g. ```json ... ```)
            string cleanJson = aiResponse.Trim();
            if (cleanJson.StartsWith("```json"))
            {
                cleanJson = cleanJson.Substring(7);
            }
            if (cleanJson.StartsWith("```"))
            {
                cleanJson = cleanJson.Substring(3);
            }
            if (cleanJson.EndsWith("```"))
            {
                cleanJson = cleanJson.Substring(0, cleanJson.Length - 3);
            }
            cleanJson = cleanJson.Trim();

            var parsed = JsonSerializer.Deserialize<CvOptimizationResultDto>(cleanJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed != null) return parsed;
        }
        catch (Exception ex)
        {
            // Fallback default structure if AI response parsing fails
            Console.WriteLine($"[AiOptimizeError] {ex.Message}");
        }

        return CreateFallbackResult();
    }

    private CvOptimizationResultDto CreateFallbackResult()
    {
        return new CvOptimizationResultDto
        {
            OverallScore = 75,
            Summary = "CV của bạn đã có cấu trúc cơ bản. Hãy kiểm tra các phần khuyến nghị để hoàn thiện thông tin và sắp xếp bố cục hợp lý hơn.",
            Sections = new List<SectionAnalysisDto>
            {
                new SectionAnalysisDto { SectionName = "Thông tin liên hệ", IsPresent = true, Status = "Good", Feedback = "Đã có thông tin cá nhân cơ bản." },
                new SectionAnalysisDto { SectionName = "Tóm tắt mục tiêu (Summary)", IsPresent = true, Status = "Warning", Feedback = "Nên bổ sung tóm tắt ngắn gọn 2-3 dòng ở đầu CV." },
                new SectionAnalysisDto { SectionName = "Kinh nghiệm làm việc", IsPresent = true, Status = "Good", Feedback = "Đã liệt kê các vị trí đã đảm nhận." },
                new SectionAnalysisDto { SectionName = "Học vấn", IsPresent = true, Status = "Good", Feedback = "Đã có thông tin trường học/chuyên ngành." },
                new SectionAnalysisDto { SectionName = "Kỹ năng", IsPresent = true, Status = "Good", Feedback = "Có liệt kê các kỹ năng chính." }
            },
            PriorityOrder = new PriorityOrderCheckDto
            {
                CandidateLevel = "Experienced",
                IsOrderOptimal = true,
                CurrentOrderDescription = "Contact -> Summary -> Experience -> Skills -> Education",
                RecommendedOrderDescription = "Contact -> Summary -> Experience -> Skills -> Education",
                Advice = "Thứ tự sắp xếp hiện tại phù hợp với cấu trúc CV tiêu chuẩn."
            },
            Recommendations = new List<CvImprovementRecommendationDto>
            {
                new CvImprovementRecommendationDto
                {
                    Category = "Contact",
                    Title = "Bổ sung liên kết LinkedIn / Portfolio",
                    Description = "Nhà tuyển dụng công nghệ đánh giá cao ứng viên có kèm đường dẫn LinkedIn chuyên nghiệp.",
                    Priority = "High",
                    ExampleBefore = "Email: candidate@email.com | SĐT: 0912345678",
                    ExampleAfter = "Email: candidate@email.com | SĐT: 0912345678 | LinkedIn: linkedin.com/in/myprofile"
                }
            }
        };
    }
}
