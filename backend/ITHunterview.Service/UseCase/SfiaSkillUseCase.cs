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

        public async Task<List<SfiaSkillResponseDto>> GetAllSfiaSkillsAsync(string? search)
        {
            var query = _context.SfiaSkills.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var lowerSearch = search.ToLower();
                query = query.Where(s => s.SkillCode.ToLower().Contains(lowerSearch) || 
                                         s.SkillName.ToLower().Contains(lowerSearch) || 
                                         s.Category.ToLower().Contains(lowerSearch));
            }

            var items = await query
                .Include(s => s.Levels)
                .OrderBy(s => s.Category)
                .ThenBy(s => s.Subcategory)
                .ThenBy(s => s.SkillCode)
                .ToListAsync();

            return items.Select(MapToResponseDto).ToList();
        }

        public async Task<SfiaSkillResponseDto> GetSfiaSkillByIdAsync(Guid id)
        {
            var entity = await _context.SfiaSkills
                .Include(s => s.Levels)
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id);

            if (entity == null)
            {
                throw new KeyNotFoundException("SFIA Skill not found.");
            }

            var responseDto = MapToResponseDto(entity);

            // Fallback to generic levels if missing
            if (!string.IsNullOrEmpty(entity.AvailableLevels))
            {
                var expectedLevels = entity.AvailableLevels.Split(',').Select(int.Parse).ToList();
                foreach (var lvl in expectedLevels)
                {
                    if (!responseDto.Levels.Any(l => l.Level == lvl))
                    {
                        var genericDesc = ITHunterview.Service.Constant.SfiaGenericLevels.GetFullDescription(lvl);
                        if (!string.IsNullOrEmpty(genericDesc))
                        {
                            responseDto.Levels.Add(new SfiaSkillLevelDto
                            {
                                Level = lvl,
                                Description = genericDesc
                            });
                        }
                    }
                }
                // Re-sort levels after injecting fallbacks
                responseDto.Levels = responseDto.Levels.OrderBy(l => l.Level).ToList();
            }

            return responseDto;
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
                Description = dto.Description,
                AvailableLevels = dto.AvailableLevels,
                Levels = dto.Levels.Select(l => new SfiaSkillLevel
                {
                    Level = l.Level,
                    Description = l.Description
                }).ToList()
            };

            _context.SfiaSkills.Add(entity);
            await _context.SaveChangesAsync();

            return MapToResponseDto(entity);
        }

        public async Task<SfiaSkillResponseDto> UpdateSfiaSkillAsync(Guid id, UpdateSfiaSkillDto dto)
        {
            var entity = await _context.SfiaSkills.Include(s => s.Levels).FirstOrDefaultAsync(s => s.Id == id);
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
            entity.AvailableLevels = dto.AvailableLevels;

            // Update levels
            _context.SfiaSkillLevels.RemoveRange(entity.Levels);
            entity.Levels = dto.Levels.Select(l => new SfiaSkillLevel
            {
                Level = l.Level,
                Description = l.Description
            }).ToList();

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

                    var skillsList = await _context.SfiaSkills.Include(s => s.Levels).ToListAsync();
                    var existingSkills = skillsList
                        .GroupBy(s => s.SkillCode.ToLower())
                        .ToDictionary(g => g.Key, g => g.First());

                    var parsedRows = new List<string[]>();
                    while (!parser.EndOfData)
                    {
                        var fields = parser.ReadFields();
                        if (fields != null && fields.Length >= 4)
                        {
                            parsedRows.Add(fields);
                        }
                    }

                    // Deduplicate by SkillCode (taking the last occurrence in the file)
                    var uniqueRows = parsedRows
                        .Where(f => !string.IsNullOrWhiteSpace(f[0]))
                        .GroupBy(f => f[0].Trim().ToLower())
                        .Select(g => g.Last())
                        .ToList();

                    foreach (var fields in uniqueRows)
                    {
                        var code = fields[0]?.Trim();
                        var name = fields[1]?.Trim();
                        var category = fields[2]?.Trim();
                        var subcategory = fields[3]?.Trim();
                        var description = fields.Length > 4 ? fields[4]?.Trim() : string.Empty;
                        var availableLevels = fields.Length > 5 ? fields[5]?.Trim() : string.Empty;

                        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(category))
                        {
                            continue; // Skip invalid rows
                        }

                        // Extract levels 1-7
                        var levels = new List<SfiaSkillLevel>();
                        for (int i = 1; i <= 7; i++)
                        {
                            var descIdx = 5 + i; // 6 is Level1_Desc
                            var levelDesc = fields.Length > descIdx ? fields[descIdx]?.Trim() : string.Empty;
                            if (!string.IsNullOrWhiteSpace(levelDesc))
                            {
                                levels.Add(new SfiaSkillLevel
                                {
                                    Level = i,
                                    Description = levelDesc
                                });
                            }
                        }

                        if (existingSkills.TryGetValue(code.ToLower(), out var existingSkill))
                        {
                            // Update existing
                            existingSkill.SkillName = name;
                            existingSkill.Category = category;
                            existingSkill.Subcategory = subcategory;
                            existingSkill.Description = description ?? string.Empty;
                            existingSkill.AvailableLevels = availableLevels ?? string.Empty;
                            
                            if (fields.Length > 6)
                            {
                                _context.SfiaSkillLevels.RemoveRange(existingSkill.Levels);
                                existingSkill.Levels = levels;
                            }
                            
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
                                Description = description ?? string.Empty,
                                AvailableLevels = availableLevels ?? string.Empty,
                                Levels = levels
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
                Description = entity.Description,
                AvailableLevels = entity.AvailableLevels,
                Levels = entity.Levels?.Select(l => new SfiaSkillLevelDto
                {
                    Id = l.Id,
                    Level = l.Level,
                    Description = l.Description
                }).OrderBy(l => l.Level).ToList() ?? new List<SfiaSkillLevelDto>()
            };
        }
    }
}
