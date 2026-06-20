using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class JobCategoriesUseCase : IJobCategoriesUseCase
    {
        private readonly IJobCategoryRepository _jobCategoryRepository;

        public JobCategoriesUseCase(IJobCategoryRepository jobCategoryRepository)
        {
            _jobCategoryRepository = jobCategoryRepository;
        }

        public async Task<ResponseBase<List<CategoryDto>>> GetCategoriesAsync()
        {
            var categories = await _jobCategoryRepository.GetCategoriesAsync();
            var dtos = categories.Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                ParentId = c.ParentId
            }).ToList();

            return new ResponseBase<List<CategoryDto>>(dtos);
        }
    }
}
