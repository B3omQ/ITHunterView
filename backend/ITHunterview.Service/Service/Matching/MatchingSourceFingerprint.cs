using System.Security.Cryptography;
using System.Text;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Stable fingerprints used only to prevent a completed matching job from
/// overwriting a source changed after its immutable snapshot was taken.
/// </summary>
public static class MatchingSourceFingerprint
{
    private const string EmptyMarker = "<empty>";

    public static string ForCv(string? normalizedFileUrl, string? rawText) =>
        Hash("matching-source/cv/v1", Normalize(normalizedFileUrl), Normalize(rawText));

    public static string ForJd(JobAnalysisInputSnapshot input, IJobAnalysisInputBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(builder);

        return Hash("matching-source/jd/v1", builder.ComputeSemanticHash(input));
    }

    public static string ForAnalysis(string? analysisJson) =>
        Hash("matching-source/analysis/v1", Normalize(analysisJson));

    private static string Hash(params string[] values)
    {
        var payload = string.Join("\u001f", values.Select(value => value ?? EmptyMarker));
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(payload)));
    }

    private static string Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? EmptyMarker : value.Normalize(NormalizationForm.FormKC).Trim();
}
