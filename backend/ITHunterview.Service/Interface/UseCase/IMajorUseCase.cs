using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IMajorUseCase
    {
        Task<ResponseBase<PagedResult<MajorDto>>> GetPagedMajorsAsync(int page, int pageSize, string? search);
        Task<ResponseBase<MajorDto>> CreateMajorAsync(CreateMajorDto dto, Guid userId);
        Task<ResponseBase<MajorDto>> UpdateMajorAsync(int id, UpdateMajorDto dto, Guid userId);
        Task<ResponseBase> DeleteMajorAsync(int id, Guid userId);
        Task<ResponseBase<MajorDto>> RestoreMajorAsync(int id, Guid userId);
        Task<ResponseBase<PagedResult<MajorDto>>> GetMajorTreeAsync(int page, int pageSize, string? search);
    }
}
