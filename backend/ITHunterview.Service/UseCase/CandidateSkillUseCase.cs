using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.CandidateProfile;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.UseCase;

namespace ITHunterview.Service.UseCase
{
    public class CandidateSkillUseCase : ICandidateSkillUseCase
    {
        private readonly ICandidateSkillRepository _skillRepo;

        public CandidateSkillUseCase(ICandidateSkillRepository skillRepo)
        {
            _skillRepo = skillRepo;
        }

        public async Task<(List<SkillResponseDto> Skills, int TotalCount)> GetSkillsAsync(Guid userId)
        {
            var userSkills = await _skillRepo.GetByUserIdAsync(userId);

            var dtos = userSkills.Select(us => new SkillResponseDto
            {
                SkillId = us.SkillId,
                Name = us.Skill?.Name ?? string.Empty,
                ProficiencyLevel = us.ProficiencyLevel
            }).ToList();

            return (dtos, dtos.Count);
        }

        public async Task<List<SkillSearchResponseDto>> SearchSkillsAsync(string keyword, bool excludeOwned, Guid userId)
        {
            if (string.IsNullOrWhiteSpace(keyword))
                return new List<SkillSearchResponseDto>();

            var excludeIds = new List<int>();
            if (excludeOwned)
            {
                var owned = await _skillRepo.GetByUserIdAsync(userId);
                excludeIds = owned.Select(us => us.SkillId).ToList();
            }

            var skills = await _skillRepo.SearchMasterSkillsAsync(keyword.Trim(), excludeIds);

            return skills.Select(s => new SkillSearchResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                CategoryId = s.CategoryId
            }).ToList();
        }

        public async Task<List<SkillSearchResponseDto>> GetAllActiveMasterSkillsAsync()
        {
            var skills = await _skillRepo.GetAllActiveMasterSkillsAsync();
            return skills.Select(s => new SkillSearchResponseDto
            {
                Id = s.Id,
                Name = s.Name,
                CategoryId = s.CategoryId
            }).ToList();
        }

        public async Task<SkillResponseDto> AddSkillAsync(Guid userId, SkillAddRequestDto request)
        {
            // Validate skill tồn tại trong master data
            var masterSkill = await _skillRepo.GetMasterSkillByIdAsync(request.SkillId);
            if (masterSkill == null)
                throw new KeyNotFoundException($"Skill với Id={request.SkillId} không tồn tại trong hệ thống.");

            // Kiểm tra user đã có skill này chưa
            var existing = await _skillRepo.GetUserSkillAsync(userId, request.SkillId);
            if (existing != null)
                throw new ArgumentException($"Bạn đã thêm skill '{masterSkill.Name}' vào profile rồi.");

            var userSkill = new UserSkills
            {
                UserId = userId,
                SkillId = request.SkillId,
                ProficiencyLevel = request.ProficiencyLevel
            };

            await _skillRepo.AddAsync(userSkill);

            return new SkillResponseDto
            {
                SkillId = masterSkill.Id,
                Name = masterSkill.Name,
                ProficiencyLevel = request.ProficiencyLevel
            };
        }

        public async Task<bool> RemoveSkillAsync(Guid userId, int skillId)
        {
            var removed = await _skillRepo.RemoveAsync(userId, skillId);
            if (!removed)
                throw new KeyNotFoundException($"Skill Id={skillId} không tồn tại trong profile của bạn.");

            return true;
        }
    }
}
