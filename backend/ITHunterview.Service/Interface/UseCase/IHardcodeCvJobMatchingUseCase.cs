using System;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IHardcodeCvJobMatchingUseCase
    {
        Task MatchCvWithAllJobsHardcodeAsync(Guid cvId);
        Task MatchJobWithAllCvsHardcodeAsync(Guid jobId);
    }
}
