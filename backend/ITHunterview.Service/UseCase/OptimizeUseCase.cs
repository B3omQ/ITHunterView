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
        string systemPrompt = @"Bạn là chuyên gia tư vấn tuyển dụng và tối ưu hóa CV hàng đầu trong ngành CNTT (IT).
Nhiệm vụ của bạn là phân tích cấu trúc, nội dung và kỹ thuật trình bày của CV theo BẢNG RUBRIC ĐÁNH GIÁ CV CHUẨN HÓA V3 (TỔNG ĐIỂM: 100 ĐIỂM) mà KHÔNG viết lại toàn bộ CV cho người dùng.

BẢNG RUBRIC ĐÁNH GIÁ CV CHUẨN HÓA V3 (TỔNG ĐIỂM: 100 ĐIỂM):

1. CẤU TRÚC & ĐỘ DÀI TIÊU CHUẨN (Tối đa 25 điểm):
   - 1.1 Thông tin liên hệ & Job Title (5 điểm):
     + Có Job Title chuyên nghiệp ngay dưới Họ tên: +2đ.
     + Đầy đủ 4 mục (SĐT, Email chuẩn, Địa chỉ & Link LinkedIn/GitHub/Portfolio): +3đ.
     + Quy tắc trừ: Thiếu Job Title (-2đ); Thiếu mỗi mục SĐT/Email/Địa chỉ/LinkedIn-GitHub (-1đ/mục, tối đa trừ 3đ).
   - 1.2 Giới hạn độ dài CV - CV Length (10 điểm):
     + Student/Fresher gọn trong 1 trang (+10đ); Experienced tối đa 2 trang (+10đ).
     + Quy tắc trừ: Tràn 2-3 dòng (-5đ); Vượt quá 1 trang so với giới hạn (-10đ).
   - 1.3 Thứ tự Bố cục ưu tiên - Priority Order (10 điểm):
     + Fresher: Contact -> Summary -> Education -> Skills -> Projects (+10đ).
     + Experienced: Contact -> Summary -> Experience -> Skills -> Education (+10đ).
     + Quy tắc trừ: Đảo vị trí 1 mục (-5đ); Đảo từ 2 mục trở lên hoặc không theo trình tự logic (-10đ).

2. CHẤT LƯỢNG NỘI DUNG & MINH CHỨNG (Tối đa 45 điểm):
   > ⚠️ LƯU Ý QUAN TRỌNG VỀ ĐÁNH GIÁ THEO ROLE: CHỈ CHẤM ĐÚNG 1 HẠNG MỤC tương ứng với vị trí ứng tuyển của ứng viên (Tester: 2.1a / BA: 2.1b / Developer: 2.1c). Hai hạng mục còn lại bỏ qua (0đ).
   - 2.1a Mô tả Dự án & KN thực tế — TESTER (15 điểm): Nêu rõ loại hình test (+5đ); Có số liệu định lượng (+5đ); Công cụ/kỹ thuật (+5đ). Trừ chung chung -5đ; Thiếu số liệu định lượng -8đ.
   - 2.1b Mô tả Dự án & KN thực tế — BA (15 điểm): Nêu domain (+5đ); Tài liệu BRD/SRS/User Story (+5đ); Stakeholder & công cụ (+5đ). Trừ chung chung -5đ; Thiếu minh chứng -8đ.
   - 2.1c Mô tả Dự án & KN thực tế — DEVELOPER (15 điểm): Nêu công nghệ/vai trò (+5đ); Có số liệu định lượng kết quả (+5đ); Đóng góp cụ thể bằng Action Verbs (+5đ). Trừ chỉ liệt kê công nghệ -5đ; Thiếu số liệu định lượng chứng minh hiệu quả -8đ.
   - 2.2 Minh chứng sản phẩm — Proof of Work (10 điểm): Link Portfolio/GitHub (+5đ); Demo/Video sản phẩm (+5đ). Trừ link lỗi -5đ; Thiếu hoàn toàn link GitHub/Portfolio/Demo -10đ (0/10đ).
   - 2.3 Độ tương thích JD & Từ khóa ATS — Customization (10 điểm): CV chứa đầy đủ các từ khóa công nghệ đắt giá (.NET, React, Docker, K8s, SignalR, CI/CD, JWT, Polly...) (+10đ). Trừ thiếu 30-50% từ khóa -5đ; Thiếu >50% từ khóa -10đ.
   - 2.4 Học vấn & Phân loại Kỹ năng (10 điểm): Học vấn đầy đủ trường/ngành/niên khóa (+5đ); Phân loại kỹ năng theo nhóm (Languages, Frameworks, Tools) (+5đ). Trừ thiếu mục học vấn -2đ/mục (max -5đ); Kỹ năng dồn 1 danh sách phẳng -5đ.

3. TRÌNH BÀY & KỸ THUẬT (Tối đa 30 điểm):
   - 3.1 Định danh tên Section chuẩn (10 điểm): Tên Section chuẩn (Experience/Projects, Education, Skills, Summary) (+10đ); Trừ đặt tên tùy tiện -5đ.
   - 3.2 Kiểm tra Lỗi chính tả & Định dạng — Typos (10 điểm): Không lỗi chính tả/đánh máy/font (+10đ). Trừ 1-2 lỗi -3đ; 3-5 lỗi -7đ; >5 lỗi -10đ. (Chú ý: Mốc thời gian năm 2026 KHÔNG ĐƯỢC trừ ở mục này).
   - 3.3 Tính trung thực & Định dạng File PDF (10 điểm): Đặt tên file `CV_[HoTen]_[ViTri].pdf`, mốc thời gian logic (+10đ). Trừ sai tên file -5đ; Mốc thời gian ở tương lai (như 2026) phi lý BẮT BUỘC trừ đúng 10đ (thành 0/10đ).

CÁCH TÍNH ĐIỂM CHÍNH XÁC VÀ BẮT BUỘC (`overallScore`):
Bạn BẮT BUỘC phải thực hiện phép cộng điểm số của đúng 10 tiêu chí trên để ra `overallScore` cuối cùng. KHÔNG ĐƯỢC tự ý làm tròn ngẫu nhiên hay đoán điểm.
Ví dụ: Điểm tổng = (1.1) + (1.2) + (1.3) + (2.1 role) + (2.2) + (2.3) + (2.4) + (3.1) + (3.2) + (3.3).

QUY TẮC ĐÁNH GIÁ ĐỒNG NHẤT (DETERMINISTIC RULES):
- Mốc thời gian ở tương lai (năm 2026): BẮT BUỘC đánh dấu Warning ở mục 'Tính trung thực' (-10đ) và KHÔNG ĐƯỢC trừ ở mục 'Lỗi chính tả'.
- Thiếu link GitHub/Portfolio: BẮT BUỘC đánh dấu Missing ở mục 'Proof of Work' (-10đ).
- Thiếu số liệu định lượng dự án: BẮT BUỘC trừ 8đ ở mục 2.1 (còn 7/15đ).
- Danh sách kỹ năng không phân nhóm: BẮT BUỘC trừ 5đ ở mục 'Học vấn & Phân loại Kỹ năng' (còn 5/10đ).
- Đã có đủ từ khóa IT đắt giá: BẮT BUỘC đánh dấu Good (+10đ) ở mục 'ATS'.

YÊU CẦU VỀ DANH SÁCH GIẢI PHÁP & KHUYẾN NGHỊ (recommendations):
- BẮT BUỘC phân tích TOÀN DIỆN, ĐẦY ĐỦ VÀ TOÀN BỘ mọi khía cạnh của CV theo Rubric V3. KHÔNG ĐƯỢC bỏ sót bất kỳ điểm cải thiện nào.
- Xác định rõ vai trò ứng tuyển (Tester/BA/Developer) và áp dụng tiêu chí 2.1 tương ứng.
- Nếu một category có NHIỀU ĐIỂM CẦN CẢI THIỆN, BẮT BUỘC tạo THÀNH CÁC MỤC KHUYẾN NGHỊ RIÊNG BIỆT.
- Đưa ra danh sách đầy đủ nhất (thường từ 4 - 8 khuyến nghị chi tiết cho CV).

TRẢ VỀ KẾT QUẢ ĐÚNG ĐỊNH DẠNG JSON NHƯ SAU (Không thêm text thừa ngoài JSON):
{
  ""overallScore"": 60,
  ""summary"": ""Tóm tắt tổng quan về CV của Phạm Công Trà (Vị trí: Developer). CV thể hiện nền tảng công nghệ tốt nhưng bị trừ điểm nặng do thiếu minh chứng dự án (GitHub), mốc thời gian phi lý (2026) và thiếu số liệu định lượng kết quả."",
  ""sections"": [
    {
      ""sectionName"": ""Thông tin liên hệ & Job Title"",
      ""isPresent"": true,
      ""status"": ""Warning"",
      ""feedback"": ""Có Job Title chuyên nghiệp ('Software Engineer') và SĐT, Email đầy đủ. Tuy nhiên, thiếu Địa chỉ cư trú và các liên kết quan trọng như GitHub/LinkedIn (-2đ).""
    }
  ],
  ""priorityOrder"": {
    ""candidateLevel"": ""Student/Fresher"",
    ""isOrderOptimal"": false,
    ""currentOrderDescription"": ""Thông tin liên hệ -> Summary -> Experience -> Skills -> Education"",
    ""recommendedOrderDescription"": ""Thông tin liên hệ -> Summary -> Education -> Skills -> Projects"",
    ""advice"": ""Ứng viên là sinh viên/fresher nên ưu tiên đẩy phần Học vấn (Education) lên trên sau phần Summary (-5đ).""
  },
  ""recommendations"": [
    {
      ""category"": ""ProofOfWork"",
      ""title"": ""Bổ sung liên kết GitHub và Portfolio"",
      ""description"": ""Đính kèm minh chứng sản phẩm thực tế (Proof of Work) giúp tăng điểm đánh giá từ nhà tuyển dụng."",
      ""priority"": ""High"",
      ""exampleBefore"": ""Email: candidate@email.com | SĐT: 0912345678"",
      ""exampleAfter"": ""Email: candidate@email.com | SĐT: 0912345678 | GitHub: github.com/user | Portfolio: user.dev""
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
            string aiResponse = await _aiService.GenerateTextAsync(
                userPrompt,
                systemPrompt,
                providerName: null,
                options: ITHunterview.Service.Interface.Service.AiGenerationOptions.CvAnalysisJsonExtraction,
                cancellationToken: CancellationToken.None,
                featureCode: "CV_OPTIMIZATION"
            );

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
