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
                    TargetLevel = rs.TargetLevel
                }).ToList()
            };
        }
    }
}
