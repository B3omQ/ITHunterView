using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.DTOs.Common;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Interface.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.VisualBasic.FileIO;
using System.IO;

namespace ITHunterview.Service.UseCase
{
    public class TargetRoleUseCase : ITargetRoleUseCase
    {
        private readonly ITHunterviewContext _context;

        public TargetRoleUseCase(ITHunterviewContext context)
        {
            _context = context;
        }

        public async Task<PagedTargetRoleResponseDto> GetPagedRolesAsync(int page, int pageSize, string? search)
        {
            var query = _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .ThenInclude(rs => rs.SfiaSkill)
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(t => t.RoleName.Contains(search) || t.Description.Contains(search));
            }

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            
            var items = await query
                .OrderBy(t => t.RoleName)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var responseItems = items.Select(MapToResponseDto).ToList();

            return new PagedTargetRoleResponseDto
            {
                TotalItems = totalItems,
                TotalPages = totalPages,
                CurrentPage = page,
                PageSize = pageSize,
                Items = responseItems
            };
        }

        public async Task<ResponseBase<TargetRoleResponseDto>> CreateRoleAsync(CreateTargetRoleTemplateDto dto)
        {
            var entity = new TargetRoleTemplate
            {
                RoleName = dto.RoleName,
                Description = dto.Description,
                RequiredSkills = dto.RequiredSkills.Select(rs => new TargetRoleSkill
                {
                    SfiaSkillId = rs.SfiaSkillId,
                    TargetLevel = rs.TargetLevel
                }).ToList()
            };

            _context.TargetRoleTemplates.Add(entity);
            await _context.SaveChangesAsync();

            // Fetch again to get the nested entities populated properly for the response
            var savedEntity = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .ThenInclude(rs => rs.SfiaSkill)
                .FirstOrDefaultAsync(t => t.Id == entity.Id);

            return new ResponseBase<TargetRoleResponseDto>(MapToResponseDto(savedEntity!), "Target Role created successfully.");
        }

        public async Task<ResponseBase<TargetRoleResponseDto>> UpdateRoleAsync(Guid id, UpdateTargetRoleTemplateDto dto)
        {
            var entity = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null)
            {
                throw new InvalidOperationException("Target Role not found.");
            }

            entity.RoleName = dto.RoleName;
            entity.Description = dto.Description;

            // Clear old skills and add new ones for simplicity
            _context.TargetRoleSkills.RemoveRange(entity.RequiredSkills);
            
            entity.RequiredSkills = dto.RequiredSkills.Select(rs => new TargetRoleSkill
            {
                RoleTemplateId = entity.Id,
                SfiaSkillId = rs.SfiaSkillId,
                TargetLevel = rs.TargetLevel
            }).ToList();

            await _context.SaveChangesAsync();

            var updatedEntity = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .ThenInclude(rs => rs.SfiaSkill)
                .FirstOrDefaultAsync(t => t.Id == entity.Id);

            return new ResponseBase<TargetRoleResponseDto>(MapToResponseDto(updatedEntity!), "Target Role updated successfully.");
        }

        public async Task<ResponseBase<bool>> DeleteRoleAsync(Guid id)
        {
            var entity = await _context.TargetRoleTemplates
                .Include(t => t.RequiredSkills)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (entity == null)
            {
                throw new InvalidOperationException("Target Role not found.");
            }

            _context.TargetRoleSkills.RemoveRange(entity.RequiredSkills);
            _context.TargetRoleTemplates.Remove(entity);
            await _context.SaveChangesAsync();

            return new ResponseBase<bool>(true, "Target Role deleted successfully.");
        }

        public async Task<List<SfiaSkillDto>> GetAllSfiaSkillsAsync()
        {
            var skills = await _context.Set<SfiaSkill>()
                .AsNoTracking()
                .OrderBy(s => s.SkillCode)
                .ToListAsync();

            return skills.Select(s => new SfiaSkillDto
            {
                Id = s.Id,
                SkillCode = s.SkillCode,
                SkillName = s.SkillName,
                Category = s.Category
            }).ToList();
        }

        public async Task<ResponseBase<TargetRoleImportResultDto>> ImportTargetRolesAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty.");
            }

            var result = new TargetRoleImportResultDto();
            
            var sfiaSkillsList = await _context.SfiaSkills.Select(s => new { s.SkillCode, s.Id }).ToListAsync();
            var existingSfiaSkills = sfiaSkillsList
                .GroupBy(s => s.SkillCode.ToLower())
                .ToDictionary(g => g.Key, g => g.First().Id);

            var rolesList = await _context.TargetRoleTemplates.Include(t => t.RequiredSkills).ToListAsync();
            var existingRoles = rolesList
                .GroupBy(r => r.RoleName.ToLower())
                .ToDictionary(g => g.Key, g => g.First());

            using (var stream = file.OpenReadStream())
            using (var reader = new StreamReader(stream))
            using (var parser = new TextFieldParser(reader))
            {
                parser.TextFieldType = FieldType.Delimited;
                parser.SetDelimiters(",");
                parser.HasFieldsEnclosedInQuotes = true;

                // Skip header
                if (!parser.EndOfData)
                {
                    parser.ReadFields();
                }

                    var parsedRows = new List<string[]>();
                    while (!parser.EndOfData)
                    {
                        var fields = parser.ReadFields();
                        if (fields != null && fields.Length >= 3)
                        {
                            parsedRows.Add(fields);
                        }
                    }

                    // Deduplicate by RoleName (taking the last occurrence in the file)
                    var uniqueRows = parsedRows
                        .Where(f => !string.IsNullOrWhiteSpace(f[0]))
                        .GroupBy(f => f[0].Trim().ToLower())
                        .Select(g => g.Last())
                        .ToList();

                    foreach (var fields in uniqueRows)
                    {
                        var roleName = fields[0]?.Trim();
                        var description = fields[1]?.Trim();
                        var requiredSkillsRaw = fields[2]?.Trim();

                    if (string.IsNullOrWhiteSpace(roleName))
                    {
                        continue;
                    }

                    var roleSkills = new List<TargetRoleSkill>();
                    var skillParts = requiredSkillsRaw?.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
                    
                    bool hasErrors = false;
                    foreach (var part in skillParts)
                    {
                        var kvp = part.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries);
                        if (kvp.Length == 2)
                        {
                            var skillCode = kvp[0].Trim().ToLower();
                            if (int.TryParse(kvp[1].Trim(), out int targetLevel))
                            {
                                if (existingSfiaSkills.TryGetValue(skillCode, out var sfiaSkillId))
                                {
                                    roleSkills.Add(new TargetRoleSkill
                                    {
                                        SfiaSkillId = sfiaSkillId,
                                        TargetLevel = targetLevel
                                    });
                                }
                                else
                                {
                                    result.Errors.Add($"Role '{roleName}': Unknown skill code '{kvp[0].Trim()}'.");
                                    hasErrors = true;
                                }
                            }
                        }
                    }

                    if (hasErrors && roleSkills.Count == 0)
                    {
                        continue; // skip if totally invalid
                    }

                    if (existingRoles.TryGetValue(roleName.ToLower(), out var existingRole))
                    {
                        // Update
                        existingRole.Description = description ?? "";
                        _context.TargetRoleSkills.RemoveRange(existingRole.RequiredSkills);
                        existingRole.RequiredSkills = roleSkills;
                        result.UpdatedCount++;
                    }
                    else
                    {
                        // Insert
                        var newRole = new TargetRoleTemplate
                        {
                            RoleName = roleName,
                            Description = description ?? "",
                            RequiredSkills = roleSkills
                        };
                        _context.TargetRoleTemplates.Add(newRole);
                        existingRoles[roleName.ToLower()] = newRole;
                        result.ImportedCount++;
                    }
                }
            }

            await _context.SaveChangesAsync();

            string msg = $"Imported {result.ImportedCount} roles. Updated {result.UpdatedCount} roles.";
            if (result.Errors.Any())
            {
                msg += $" Encountered {result.Errors.Count} errors (see details).";
            }

            return new ResponseBase<TargetRoleImportResultDto>(result, msg);
        }

        private TargetRoleResponseDto MapToResponseDto(TargetRoleTemplate entity)
        {
            return new TargetRoleResponseDto
            {
                Id = entity.Id,
                RoleName = entity.RoleName,
                Description = entity.Description,
                RequiredSkills = entity.RequiredSkills.Select(rs => new TargetRoleSkillDto
                {
                    SkillCode = rs.SfiaSkill?.SkillCode ?? "",
                    SkillName = rs.SfiaSkill?.SkillName ?? "",
                    Description = rs.SfiaSkill?.Description ?? "",
                    AvailableLevels = rs.SfiaSkill?.AvailableLevels ?? "",
                    TargetLevel = rs.TargetLevel
                }).ToList()
            };
        }
    }
}
