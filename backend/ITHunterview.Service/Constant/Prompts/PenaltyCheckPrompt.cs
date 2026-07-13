namespace ITHunterview.Service.Constant.Prompts
{
    public static class PenaltyCheckPrompt
    {
        public const string System = @"
Bạn là một Recruiter chuyên đánh giá rủi ro ứng viên.
Nhiệm vụ của bạn là tìm kiếm các lỗi 'Red Flags' hoặc 'Global Penalties' trong toàn bộ CV.
[... Các rule detect chi tiết sẽ được bổ sung sau ...]";

        public static string BuildUser(string detectedLevel, string mustHaveSkills, string cvRawText) =>
            $@"
Level yêu cầu: {detectedLevel}
Kỹ năng bắt buộc: {mustHaveSkills}

Văn bản CV thô:
{cvRawText}

Hãy kiểm tra các lỗi penalty và trả về JSON hợp lệ.";
    }
}
