using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Exceptions;

public sealed class CvAnalysisValidationException : InvalidOperationException
{
    public CvAnalysisValidationException(CvAnalysisValidationResult failure)
        : base(string.IsNullOrWhiteSpace(failure.FailureCode)
            ? "CV_ANALYSIS_SCHEMA_INVALID"
            : failure.FailureCode)
    {
        FailureCode = string.IsNullOrWhiteSpace(failure.FailureCode)
            ? "CV_ANALYSIS_SCHEMA_INVALID"
            : failure.FailureCode;
        DiagnosticCode = failure.DiagnosticCode;
        JsonPath = failure.JsonPath;
    }

    public string FailureCode { get; }
    public string DiagnosticCode { get; }
    public string JsonPath { get; }
}
