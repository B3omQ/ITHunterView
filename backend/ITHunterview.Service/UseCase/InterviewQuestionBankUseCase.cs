using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.InterviewQuestionBank;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.AspNetCore.Http;
using ClosedXML.Excel;

namespace ITHunterview.Service.UseCase
{
    public class InterviewQuestionBankUseCase : IInterviewQuestionBankUseCase
    {
        private readonly IInterviewQuestionBankRepository _repository;

        public InterviewQuestionBankUseCase(IInterviewQuestionBankRepository repository)
        {
            _repository = repository;
        }

        public async Task<PagedResult<QuestionBankDto>> GetPagedAsync(int pageIndex, int pageSize, string? industry, string? level)
        {
            var pagedEntities = await _repository.GetPagedAsync(pageIndex, pageSize, industry, level);
            return new PagedResult<QuestionBankDto>
            {
                Items = pagedEntities.Items.Select(e => new QuestionBankDto
                {
                    Id = e.Id,
                    CategoryId = e.CategoryId,
                    Industry = e.Industry,
                    Level = e.Level,
                    QuestionText = e.QuestionText
                }).ToList(),
                TotalCount = pagedEntities.TotalCount,
                Page = pagedEntities.Page,
                PageSize = pagedEntities.PageSize
            };
        }

        public async Task<QuestionBankDto> GetByIdAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Question not found");
            }
            return MapToDto(entity);
        }

        public async Task<QuestionBankDto> CreateAsync(CreateQuestionBankDto dto, Guid userId)
        {
            var entity = new InterviewQuestionBank
            {
                Id = Guid.NewGuid(),
                CategoryId = dto.CategoryId,
                Industry = dto.Industry,
                Level = dto.Level,
                QuestionText = dto.QuestionText,
                CreatedBy = userId,
                UpdatedBy = userId
            };

            await _repository.AddAsync(entity);
            return MapToDto(entity);
        }

        public async Task<int> ImportFromExcelAsync(string industry, string level, IFormFile file, Guid userId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("File is empty");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Only .xlsx files are supported");

            var entities = new List<InterviewQuestionBank>();

            using (var stream = file.OpenReadStream())
            using (var workbook = new XLWorkbook(stream))
            {
                var worksheet = workbook.Worksheets.FirstOrDefault();
                if (worksheet == null) throw new ArgumentException("File Excel không có dữ liệu (Worksheet trống).");

                var rows = worksheet.RowsUsed().Skip(1); // Skip header

                foreach (var row in rows)
                {
                    var questionText = row.Cell(1).GetString();

                    if (!string.IsNullOrWhiteSpace(questionText))
                    {
                        entities.Add(new InterviewQuestionBank
                        {
                            Id = Guid.NewGuid(),
                            Industry = industry,
                            Level = level,
                            QuestionText = questionText.Trim(),
                            CreatedBy = userId,
                            UpdatedBy = userId
                        });
                    }
                }
            }

            if (entities.Count == 0)
            {
                throw new ArgumentException("Vui lòng nhập dữ liệu câu hỏi ở cột A.");
            }

            await _repository.AddRangeAsync(entities);

            return entities.Count;
        }

        public async Task<QuestionBankDto> UpdateAsync(Guid id, UpdateQuestionBankDto dto, Guid userId)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Question not found");
            }

            entity.CategoryId = dto.CategoryId;
            entity.Industry = dto.Industry;
            entity.Level = dto.Level;
            entity.QuestionText = dto.QuestionText;
            entity.UpdatedBy = userId;

            await _repository.UpdateAsync(entity);
            return MapToDto(entity);
        }

        public async Task DeleteAsync(Guid id)
        {
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("Question not found");
            }

            await _repository.DeleteAsync(entity);
        }

        private QuestionBankDto MapToDto(InterviewQuestionBank entity)
        {
            return new QuestionBankDto
            {
                Id = entity.Id,
                CategoryId = entity.CategoryId,
                Industry = entity.Industry,
                Level = entity.Level,
                QuestionText = entity.QuestionText
            };
        }
    }
}
