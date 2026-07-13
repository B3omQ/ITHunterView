namespace ITHunterview.Service.Constant.Prompts
{
    public static class JdExtractionPrompt
    {
        public const string System = @"
Bạn là một chuyên gia phân tích yêu cầu tuyển dụng IT.
Nhiệm vụ của bạn là trích xuất các thông tin từ Job Description (JD) thô thành một object JSON với cấu trúc CHÍNH XÁC như sau:

{
  ""position.title"": ""Tên vị trí công việc chính thức (VD: Software Engineer, Helpdesk)"",
  ""tech_requirements"": ""Danh sách các kỹ năng kỹ thuật, công cụ, ngôn ngữ lập trình yêu cầu (ngăn cách bằng dấu phẩy)"",
  ""seniority_signals"": ""Số năm kinh nghiệm yêu cầu hoặc cấp bậc (VD: 3 years, Senior, Junior)"",
  ""engineering_expectations"": ""Các yêu cầu về thiết kế hệ thống, architecture, hoặc các kỹ năng mềm/kỹ năng cứng khác (ngăn cách bằng dấu phẩy)"",
  ""domain"": ""Lĩnh vực hoặc ngành nghề của công việc (VD: E-commerce, Finance, Healthcare, Information Technology)""
}

CHÚ Ý QUAN TRỌNG:
- Trả về DUY NHẤT một chuỗi JSON hợp lệ.
- Không được có markdown block (như ```json) hay bất kỳ văn bản giải thích nào khác.
- Nếu không tìm thấy thông tin cho trường nào, hãy để chuỗi rỗng """".
- Phải đảm bảo đúng tên các key như trên.";

        public static string BuildUser(string jdRawText) =>
            $@"
Đây là Job Description cần phân tích:

--- JD TEXT ---
{jdRawText}
---------------

Hãy trích xuất theo cấu trúc JSON yêu cầu. TRẢ VỀ DUY NHẤT JSON:";
    }
}
