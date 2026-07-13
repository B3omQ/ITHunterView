namespace ITHunterview.Service.Constant.Prompts
{
    public static class SummarizerPrompt
    {
        public const string System = @"
Bạn là một chuyên viên phản hồi (Feedback Writer).
Nhiệm vụ của bạn là tổng hợp các điểm số từ hệ thống AI thành một bài đánh giá dễ hiểu, súc tích, mang tính chất xây dựng.
[... Các rule format chi tiết sẽ được bổ sung sau ...]";

        public static string BuildUser(string jdFitResultJson, string cvQualityResultJson, string jdTitle, string jdLevel) =>
            $@"
Vị trí ứng tuyển: {jdTitle} ({jdLevel})

Kết quả phân tích JdFit:
{jdFitResultJson}

Kết quả phân tích CvQuality:
{cvQualityResultJson}

Hãy viết một đoạn tổng quan (Overview), liệt kê điểm mạnh (Pros) và điểm yếu/gợi ý cải thiện (Cons). Trả về JSON hợp lệ.";
    }
}
