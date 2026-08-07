using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Exceptions;

/// <summary>
/// Preserves the bounded JD validator failure code without carrying provider
/// output or JD content across the matching failure boundary.
/// </summary>
public sealed class JdAnalysisValidationException : InvalidOperationException
{
    public JdAnalysisValidationException(ValidationResult<ValidatedJobAnalysis> failure)
        : base(NormalizeFailureCode(failure))
    {
        FailureCode = NormalizeFailureCode(failure);
    }

    public string FailureCode { get; }

    private static string NormalizeFailureCode(ValidationResult<ValidatedJobAnalysis> failure)
    {
        ArgumentNullException.ThrowIfNull(failure);
        return string.IsNullOrWhiteSpace(failure.FailureCode)
            ? "JD_ANALYSIS_SCHEMA_INVALID"
            : failure.FailureCode;
    }
}
