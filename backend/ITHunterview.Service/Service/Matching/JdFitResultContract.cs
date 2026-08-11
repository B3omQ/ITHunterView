namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Stable metadata written into the existing frontend-compatible result. It
/// is not used to select a prompt or a runtime validator.
/// </summary>
public static class JdFitResultContract
{
    public const string Version3 = "jd-matching/v3";
    public const string Version4 = "jd-matching/v4";
    public const string Current = Version4;
    public const string RawTextFallback = "jd-matching/raw-text-v1";
}
