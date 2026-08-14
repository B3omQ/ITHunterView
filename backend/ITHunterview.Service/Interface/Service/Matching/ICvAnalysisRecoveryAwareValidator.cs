using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface ICvAnalysisRecoveryAwareValidator
{
    CvAnalysisValidationResult ValidateRecovered(CvAnalysisRecoveryResult recovery);
    CvAnalysisValidationResult ValidateStoredCanonical(string canonicalJson);
}
