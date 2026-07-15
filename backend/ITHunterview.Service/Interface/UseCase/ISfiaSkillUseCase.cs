using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface ISfiaSkillUseCase
    {
        Task<PagedSfiaSkillResponseDto> GetPagedSfiaSkillsAsync(int page, int pageSize, string? search);
        Task<SfiaSkillResponseDto> CreateSfiaSkillAsync(CreateSfiaSkillDto dto);
        Task<SfiaSkillResponseDto> UpdateSfiaSkillAsync(Guid id, UpdateSfiaSkillDto dto);
        Task<bool> DeleteSfiaSkillAsync(Guid id);
        Task<int> ImportSfiaSkillsAsync(IFormFile file);
    }
}
