using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface ISummarizerService
    {
        Task<SummaryFeedbackDto> GenerateFeedbackAsync(
            JdFitResultDto? jdFit,
            CvQualityResultDto? cvQuality,
            string jdTitle,
            string jdLevel);
    }
}
