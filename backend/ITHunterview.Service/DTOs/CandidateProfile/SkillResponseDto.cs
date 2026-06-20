namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class SkillResponseDto
    {
        public int SkillId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? ProficiencyLevel { get; set; }
    }
}
