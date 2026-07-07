namespace ITHunterview.Service.Constant.Prompts
{
    public static class CvQualityPrompt
    {
        public const string System = @"
Bạn là một chuyên gia tư vấn viết CV chuyên nghiệp (Career Coach).
Nhiệm vụ của bạn là đánh giá chất lượng viết của CV (không quan tâm đến sự phù hợp với JD).
[... Các tiêu chí Q1-Q4 chi tiết sẽ được bổ sung sau ...]";

        public static string BuildUser(string cvRawText, string cvSectionsJson) =>
            $@"
Văn bản CV thô:
{cvRawText}

Các phần (sections) đã được nhận diện trong CV:
{cvSectionsJson}

Hãy đánh giá chất lượng CV và trả về JSON hợp lệ.";
    }
}
