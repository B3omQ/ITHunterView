using System;

namespace ITHunterview.Service.DTOs.Cv
{
    public class CvJobMatchScoreResponseDto
    {
        public Guid Id { get; set; }
        public Guid CvId { get; set; }
        public Guid? JobId { get; set; }
        public decimal OverallScore { get; set; }
        public decimal SkillMatchScore { get; set; }
        public decimal ExperienceMatchScore { get; set; }
        public decimal DomainMatchScore { get; set; }
        public string MatchDetails { get; set; } = string.Empty;
    }
}
