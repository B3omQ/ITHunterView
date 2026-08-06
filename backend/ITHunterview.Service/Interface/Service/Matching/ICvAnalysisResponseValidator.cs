using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface ICvAnalysisResponseValidator
{
    CvAnalysisValidationResult ValidateAndCanonicalize(string responseJson, CvAnalysisInputSnapshot input);
}
