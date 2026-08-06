namespace ITHunterview.Service.Constant.Prompts
{
    public static class CvAnalysisPromptContract
    {
        public const string SystemPromptKey = "CV_ANALYSIS_SYSTEM";
        public const string UserPromptKey = "CV_ANALYSIS_USER";
        public const string UserPlaceholder = "[CV_TEXT]";
        public const string ContractV1 = "cv-analysis/v1";
        public const string SystemRole = "system";
        public const string UserRole = "user";

        public static bool IsCvAnalysisPromptKey(string promptKey)
        {
            return promptKey == SystemPromptKey || promptKey == UserPromptKey;
        }
    }
}
