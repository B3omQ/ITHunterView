using System;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IHardcodeCvJobMatchingUseCase
    {
        Task MatchCvWithAllJobsHardcodeAsync(Guid cvId, Guid userId);
        Task MatchJobWithAllCvsHardcodeAsync(Guid jobId, Guid userId);
    }
}
