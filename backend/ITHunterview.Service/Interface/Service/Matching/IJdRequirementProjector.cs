using ITHunterview.Service.DTOs.Cv.Matching;

namespace ITHunterview.Service.Interface.Service.Matching;

public interface IJdRequirementProjector
{
    JdRequirementProjection Project(string? effectiveJdJson);
}
