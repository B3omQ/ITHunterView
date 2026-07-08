namespace ITHunterview.Service.Constant.Prompts
{
    public static class JdExtractionPrompt
    {
        public const string System = @"
Bạn là một chuyên gia phân tích yêu cầu tuyển dụng IT.
Nhiệm vụ của bạn là trích xuất các yêu cầu từ Job Description (JD) thô thành một mảng các đối tượng JSON.
[... Các rule trích xuất chi tiết sẽ được bổ sung sau ...]";

        public static string BuildUser(string jdRawText) =>
            $@"
Đây là Job Description cần phân tích:

{jdRawText}

Hãy trích xuất requirements và trả về JSON hợp lệ.";
    }
}
