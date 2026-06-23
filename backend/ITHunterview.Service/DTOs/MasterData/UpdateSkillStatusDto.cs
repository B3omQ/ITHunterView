using ITHunterview.Domain.Enums;

namespace ITHunterview.Service.DTOs.MasterData
{
    public class UpdateSkillStatusDto
    {
        public SkillStatus Status { get; set; }
        public bool Force { get; set; } = false;
    }
}
