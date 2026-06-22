using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.JobApplication;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IJobApplicationUseCase
    {
        Task<PagedResult<ApplicantDto>> GetApplicantsByJobIdAsync(Guid jobId, int page, int pageSize);
    }
}
