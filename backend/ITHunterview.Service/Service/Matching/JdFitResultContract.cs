namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Stable metadata written into the existing frontend-compatible result. It
/// is not used to select a prompt or a runtime validator.
/// </summary>
public static class JdFitResultContract
{
    public const string Version3 = "jd-matching/v3";
    public const string Version4 = "jd-matching/v4";
    // Application-owned persisted result contracts. They are not prompt-pair
    // metadata and never select a prompt, provider schema, or algorithm.
    public const string Version5 = "jd-matching/v5";
    public const string Current = Version5;
    public const string RawTextFallback = "jd-matching/raw-text-v1";
    public const string RawTextFallbackVersion2 = "jd-matching/raw-text-v2";
}
