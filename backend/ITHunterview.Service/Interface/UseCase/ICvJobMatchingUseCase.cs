using System;
using System.Threading.Tasks;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ICvJobMatchingUseCase
    {
        Task MatchCvWithAllJobsAsync(Guid cvId);
        Task MatchJobWithAllCvsAsync(Guid jobId);
    }
}
