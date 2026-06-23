using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IJobCategoriesUseCase
    {
        Task<ResponseBase<List<CategoryDto>>> GetCategoriesAsync();
    }
}
