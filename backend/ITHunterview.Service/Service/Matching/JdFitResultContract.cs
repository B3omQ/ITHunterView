namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Stable metadata written into the existing frontend-compatible result. It
/// is not used to select a prompt or a runtime validator.
/// </summary>
public static class JdFitResultContract
{
    public const string Current = "jd-matching/v3";
    public const string RawTextFallback = "jd-matching/raw-text-v1";
}
