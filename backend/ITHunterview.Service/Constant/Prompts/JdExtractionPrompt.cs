namespace ITHunterview.Service.Constant.Prompts
{
    public static class JdExtractionPrompt
    {
        public const string System = @"
Bạn là một chuyên gia phân tích yêu cầu tuyển dụng IT.
Nhiệm vụ của bạn là trích xuất các thông tin từ Job Description (JD) thô thành một object JSON với cấu trúc Hybrid (vừa giữ nguyên văn bản, vừa trích xuất metrics cho hệ thống Matching) CHÍNH XÁC như sau:

{
  ""verbatim_sections"": {
    ""title"": ""Copy y nguyên chức danh công việc"",
    ""description"": ""Copy y nguyên mô tả công việc"",
    ""requirements"": ""Copy y nguyên các yêu cầu công việc"",
    ""responsibilities"": ""Copy y nguyên các trách nhiệm công việc"",
    ""benefits"": ""Copy y nguyên các quyền lợi""
  },
  ""matching_metrics"": {
    ""job_titles_normalized"": [""Software Engineer"", ""Developer""], // Mảng chuỗi: Trích xuất tên vị trí công việc chính thức
    ""skills_normalized"": [""C#"", ""SQL"", ""React""], // Mảng chuỗi: Trích xuất tất cả công nghệ, kỹ năng kỹ thuật, công cụ yêu cầu
    ""total_years_exp"": 0, // Số nguyên: Tổng số năm kinh nghiệm yêu cầu tối thiểu
    ""domains"": [""Finance"", ""Banking""], // Mảng chuỗi: Trích xuất lĩnh vực hoặc ngành nghề của công việc (VD: E-commerce, Finance)
    ""requirements_list"": [
      {
        ""category"": ""Phân loại (tech_skill, experience, domain_knowledge, language, education, soft_skill)"",
        ""importance"": ""must_have hoặc nice_to_have"",
        ""skill_name"": ""Tên kỹ năng hoặc yêu cầu ngắn gọn"",
        ""detail_verbatim"": ""Mô tả chi tiết yêu cầu này (trích xuất nguyên văn từ JD)""
      }
    ]
  }
}

CHÚ Ý QUAN TRỌNG:
- Trong `verbatim_sections`, bạn PHẢI giữ nguyên từng từ của văn bản gốc, KHÔNG tóm tắt.
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
