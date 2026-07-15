using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.VisualBasic.FileIO;

namespace ITHunterview.Service.UseCase
{
    public class SfiaSkillUseCase : ISfiaSkillUseCase
    {
        private readonly ITHunterviewContext _context;

        public SfiaSkillUseCase(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<PagedSfiaSkillResponseDto> GetPagedSfiaSkillsAsync(int page, int pageSize, string? search)
        {
            var query = _context.SfiaSkills.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.SkillCode.ToLower().Contains(lowerSearch) || 
                                         s.SkillName.ToLower().Contains(lowerSearch) || 
                                         s.Category.ToLower().Contains(lowerSearch));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var items = await query
                .OrderBy(s => s.SkillCode)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseItems = items.Select(MapToResponseDto).ToList();

            return new PagedSfiaSkillResponseDto
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Items = responseItems
            };
        }

        public async Task<SfiaSkillResponseDto> CreateSfiaSkillAsync(CreateSfiaSkillDto dto)
        {
            var exists = await _context.SfiaSkills.AnyAsync(s => s.SkillCode == dto.SkillCode);
            if (exists)
            {
                throw new ArgumentException($"SFIA Skill with code '{dto.SkillCode}' already exists.");
            }

            var entity = new SfiaSkill
            {
                SkillCode = dto.SkillCode,
                SkillName = dto.SkillName,
                Category = dto.Category,
                Subcategory = dto.Subcategory,
                Description = dto.Description
            };

            _context.SfiaSkills.Add(entity);
            await _context.SaveChangesAsync();

            return MapToResponseDto(entity);
        }

        public async Task<SfiaSkillResponseDto> UpdateSfiaSkillAsync(Guid id, UpdateSfiaSkillDto dto)
        {
            var entity = await _context.SfiaSkills.FindAsync(id);
            if (entity == null)
            {
                throw new KeyNotFoundException("SFIA Skill not found.");
            }

            // Check if changing to a code that already exists on another skill
            var codeExists = await _context.SfiaSkills.AnyAsync(s => s.SkillCode == dto.SkillCode && s.Id != id);
            if (codeExists)
            {
                throw new ArgumentException($"SFIA Skill with code '{dto.SkillCode}' already exists.");
            }

            entity.SkillCode = dto.SkillCode;
            entity.SkillName = dto.SkillName;
            entity.Category = dto.Category;
            entity.Subcategory = dto.Subcategory;
            entity.Description = dto.Description;

            await _context.SaveChangesAsync();

            return MapToResponseDto(entity);
        }

        public async Task<bool> DeleteSfiaSkillAsync(Guid id)
        {
            var entity = await _context.SfiaSkills.Include(s => s.TargetRoleSkills).FirstOrDefaultAsync(s => s.Id == id);
            if (entity == null)
            {
                throw new KeyNotFoundException("SFIA Skill not found.");
            }

            if (entity.TargetRoleSkills != null && entity.TargetRoleSkills.Any())
            {
                throw new ArgumentException("Cannot delete SFIA Skill because it is being used by one or more Target Roles.");
            }

            _context.SfiaSkills.Remove(entity);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<int> ImportSfiaSkillsAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty.");
            }

            var importedCount = 0;
            var updatedCount = 0;

            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                // Simple CSV parsing using TextFieldParser for robust comma separation handling quotes
                using (var parser = new TextFieldParser(stream))
                {
                    parser.TextFieldType = FieldType.Delimited;
                    parser.SetDelimiters(",");
                    parser.HasFieldsEnclosedInQuotes = true;

                    // Skip header
                    if (!parser.EndOfData)
                    {
                        parser.ReadFields();
                    }

                    var existingSkills = await _context.SfiaSkills.ToDictionaryAsync(s => s.SkillCode.ToLower());

                    while (!parser.EndOfData)
                    {
                        var fields = parser.ReadFields();
                        if (fields == null || fields.Length < 4) continue;

                        var code = fields[0]?.Trim();
                        var name = fields[1]?.Trim();
                        var category = fields[2]?.Trim();
                        var subcategory = fields[3]?.Trim();
                        var description = fields.Length > 4 ? fields[4]?.Trim() : string.Empty;

                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
                        {
                            continue; // Skip invalid rows
                        }

                        if (existingSkills.TryGetValue(code.ToLower(), out var existingSkill))
                        {
                            // Update existing
                            existingSkill.SkillName = name;
                            existingSkill.Category = category;
                            existingSkill.Subcategory = subcategory;
                            existingSkill.Description = description ?? string.Empty;
                            updatedCount++;
                        }
                        else
                        {
                            // Insert new
                            var newSkill = new SfiaSkill
                            {
                                SkillCode = code,
                                SkillName = name,
                                Category = category,
                                Subcategory = subcategory,
                                Description = description ?? string.Empty
                            };
                            _context.SfiaSkills.Add(newSkill);
                            importedCount++;
                            // Add to dictionary to prevent duplicates in the same file
                            existingSkills[code.ToLower()] = newSkill;
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            return importedCount + updatedCount;
        }

        private SfiaSkillResponseDto MapToResponseDto(SfiaSkill entity)
        {
            return new SfiaSkillResponseDto
            {
                Id = entity.Id,
                SkillCode = entity.SkillCode,
                SkillName = entity.SkillName,
                Category = entity.Category,
                Subcategory = entity.Subcategory,
                Description = entity.Description
            };
        }
    }
}
