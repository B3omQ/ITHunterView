using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IJdStageTwoMatchingService
{
    Task<JdFitScoreCalculation> ExecuteAsync(
        PromptSnapshotDto activePrompt,
        string cvContextJson,
        JdRequirementProjection jdProjection,
        CancellationToken cancellationToken = default);

    JdFitScoreCalculation CreateConfigurationUnavailableResult(
        JdRequirementProjection jdProjection);
}
