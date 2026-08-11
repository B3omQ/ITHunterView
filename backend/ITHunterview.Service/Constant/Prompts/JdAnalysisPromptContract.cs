namespace ITHunterview.Service.Constant.Prompts;

public static class JdAnalysisPromptContract
{
    // The keys stay unchanged so existing v2 histories and analysis runs remain addressable.
    public const string SystemPromptKey = "JD_ANALYSIS_V2_SYSTEM";
    public const string UserPromptKey = "JD_ANALYSIS_V2_USER";
    public const string UserPlaceholder = "[JOB_INPUT_JSON]";
    public const string ContractV2 = "jd-analysis/v2";
    public const string ContractV3 = "jd-analysis/v3";
    public const string ContractV4 = "jd-analysis/v4";
    public const string CurrentContract = ContractV4;
    public const string SystemRole = "system";
    public const string UserRole = "user";

    public static bool IsJdAnalysisPromptKey(string promptKey) =>
        promptKey == SystemPromptKey || promptKey == UserPromptKey;
}
