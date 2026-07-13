# Hướng Dẫn Tích Hợp và Sử Dụng AI Service (Gemini, OpenAI, Claude)

Tài liệu này hướng dẫn cách hoạt động của hệ thống AI Provider Switching và cách các thành viên phát triển tính năng liên quan đến AI (như CV Optimizer, Match Score, AI Interview) sử dụng các service có sẵn.

---

## 1. Kiến Trúc Hoạt Động (Architecture)

Hệ thống được thiết kế theo mẫu **Strategy Pattern** phối hợp với **Factory Pattern** để hỗ trợ chuyển đổi nhà cung cấp AI tại runtime mà không cần khởi động lại ứng dụng.

```
                        [ Controllers / Use Cases ]
                                     │
                             [ IAiService ]
                                     │
                     [ IAiProviderFactory ]
                                     │
             ┌───────────────────────┼───────────────────────┐
             ▼                       ▼                       ▼
      [ GeminiProvider ]     [ OpenAiProvider ]      [ ClaudeProvider ]
```

- **AiSettings (appsettings.json):** Lưu trữ thông tin nhạy cảm và thông số kỹ thuật (API Key, Model, API URL) của từng nhà cung cấp.
- **Database (system_configs):** Lưu cấu hình nhà cung cấp nào đang được chọn chạy chính (`ActiveAiProvider`) để có thể thay đổi động bằng API.
- **IAiProvider:** Interface định nghĩa cách một nhà cung cấp AI cụ thể call HTTP API.
- **IAiProviderFactory:** Bộ giải quyết (Resolver) tìm đúng Provider class theo tên được yêu cầu.
- **IAiService:** Lớp bọc (Wrapper) chính chịu trách nhiệm đọc DB để lấy Active Provider, gọi Factory để xử lý sinh văn bản, và tự động ghi log sử dụng vào bảng `ai_api_usage_logs`.

---

## 2. Hướng Dẫn Sử Dụng Trong Use Cases (Dành cho Developer)

Khi viết các Use Cases cần gọi AI (ví dụ: So khớp CV, Viết gợi ý, Tạo câu hỏi phỏng vấn), bạn **chỉ cần inject `IAiService`** vào constructor. Hệ thống tự động hoán chuyển model (Gemini, OpenAI, Claude) đằng sau dựa theo cấu hình của Admin.

### Ví dụ Code C# thực tế:

```csharp
using System.Threading.Tasks;
using ITHunterview.Service.Interface.Service;

namespace ITHunterview.Service.UseCase
{
    public class CvOptimizeUseCase
    {
        private readonly IAiService _aiService;

        public CvOptimizeUseCase(IAiService aiService)
        {
            _aiService = aiService;
        }

        public async Task<string> OptimizeResumeAsync(string resumeText)
        {
            var systemPrompt = "Bạn là chuyên gia tuyển dụng IT. Hãy tối ưu hóa CV sau để thu hút nhà tuyển dụng.";
            var prompt = $"Nội dung CV cần tối ưu:\n{resumeText}";

            // Gọi GenerateTextAsync để tự động gọi model AI đang được kích hoạt hệ thống
            string optimizedResult = await _aiService.GenerateTextAsync(prompt, systemPrompt);

            return optimizedResult;
        }
    }
}
```

- **Hàm `GenerateTextAsync(prompt, systemPrompt)`:**
  - `prompt`: Câu lệnh gửi cho AI.
  - `systemPrompt` (Tùy chọn): Định hình vai trò, ngữ cảnh cho AI (System Instruction).

---

## 3. Các API Quản Lý AI (Dành cho Frontend / Admin Panel)

Hệ thống cung cấp một controller [AiController.cs](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/backend/ITHunterview.WebAPI/Controllers/AiController.cs) phục vụ công tác quản trị:

| Endpoint | Method | Quyền Hạn | Mô tả |
| :--- | :--- | :--- | :--- |
| `/api/ai/configs` | `GET` | Staff / Admin | Lấy danh sách các AI hỗ trợ, xem AI nào đang kích hoạt và kiểm tra key có trống không. |
| `/api/ai/configs/active` | `POST` | Staff / Admin | Đổi nhà cung cấp AI hoạt động chính (Body: `{"providerName": "OpenAI"}`). |
| `/api/ai/test-connection` | `POST` | Staff / Admin | Gửi prompt test đến một provider bất kỳ để đo thời gian phản hồi và kiểm tra API Key. |
| `/api/ai/generate` | `POST` | Mọi user | Gọi trực tiếp sinh văn bản thông qua Active AI hiện tại. |

---

## 4. Cách Thêm Một AI Provider Mới (Ví dụ: DeepSeek)

Nếu trong tương lai nhóm muốn thêm một nhà cung cấp mới (ví dụ: DeepSeek), hãy làm theo các bước sau:

1. **Cấu hình `appsettings.json`:**
   Thêm thông tin DeepSeek vào khối `AiSettings.Providers`:
   ```json
   "DeepSeek": {
     "ApiKey": "YOUR_DEEPSEEK_API_KEY",
     "Model": "deepseek-chat",
     "Endpoint": "https://api.deepseek.com/v1/chat/completions"
   }
   ```
2. **Tạo Class Provider mới:**
   Tạo class `DeepSeekProvider.cs` kế thừa `IAiProvider` tại thư mục `Services/AiProviders/`. Implement hàm `GenerateTextAsync` bằng cách call HTTP API của họ.
3. **Đăng ký Dependency Injection:**
   Thêm dòng đăng ký tại [ServiceCollectionExtensions.cs](file:///c:/Users/LAPTOP/OneDrive/Documents/GitHub/ITHunterView_backup/backend/ITHunterview.Service/Config/ServiceCollectionExtensions.cs):
   ```csharp
   services.AddScoped<IAiProvider, DeepSeekProvider>();
   ```

*Hệ thống Factory sẽ tự động nhận diện và tích hợp DeepSeek vào danh sách chuyển đổi mà không cần sửa thêm bất kỳ dòng code nào khác.*
