using System;
using System.Threading.Tasks;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.InterviewQuestionBank;

namespace ITHunterview.Service.Interface.UseCase
{
    public interface IInterviewQuestionBankUseCase
    {
        Task<PagedResult<QuestionBankDto>> GetPagedAsync(int pageIndex, int pageSize, string? industry, string? level);
        Task<QuestionBankDto> GetByIdAsync(Guid id);
        Task<QuestionBankDto> CreateAsync(CreateQuestionBankDto dto, Guid userId);
        Task<int> ImportFromExcelAsync(string industry, string level, Microsoft.AspNetCore.Http.IFormFile file, Guid userId);
        Task<QuestionBankDto> UpdateAsync(Guid id, UpdateQuestionBankDto dto, Guid userId);
        Task DeleteAsync(Guid id);
    }
}
