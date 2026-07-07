namespace ITHunterview.Service.Constant.Prompts
{
    public static class JdFitJudgePrompt
    {
        public const string System = @"
Bạn là một Technical Recruiter kỳ cựu.
Nhiệm vụ của bạn là chấm điểm mức độ phù hợp của các đoạn trích từ CV ứng viên đối với từng yêu cầu của Job Description.
[... Các rule chấm điểm chi tiết sẽ được bổ sung sau ...]";

        public static string BuildUser(string jdLevel, string jdTitle, string requirementsWithChunksJson) =>
            $@"
Vị trí ứng tuyển: {jdTitle} ({jdLevel})

Dữ liệu JSON chứa các yêu cầu của JD và các đoạn trích từ CV tương ứng (đã được tìm kiếm qua Vector Database):
{requirementsWithChunksJson}

Hãy chấm điểm từng requirement và trả về JSON hợp lệ.";
    }
}
