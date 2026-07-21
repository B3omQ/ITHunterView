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
        Task<List<SfiaSkillResponseDto>> GetAllSfiaSkillsAsync(string? search);
        Task<SfiaSkillResponseDto> GetSfiaSkillByIdAsync(Guid id);
        Task<SfiaSkillResponseDto> CreateSfiaSkillAsync(CreateSfiaSkillDto dto);
        Task<SfiaSkillResponseDto> UpdateSfiaSkillAsync(Guid id, UpdateSfiaSkillDto dto);
        Task<bool> DeleteSfiaSkillAsync(Guid id);
        Task<int> ImportSfiaSkillsAsync(IFormFile file);
    }
}
