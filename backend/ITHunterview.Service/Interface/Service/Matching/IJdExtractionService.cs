using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching
{
    public interface IJdExtractionService
    {
        Task<JdExtractionResultDto> ExtractRequirementsAsync(string rawJdText);
    }
}
