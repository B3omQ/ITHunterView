using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class JdExtractionResultDto
    {
        public string JdTitle { get; set; } = string.Empty;
        public string JdLevel { get; set; } = string.Empty;
        public List<JdRequirementDto> Requirements { get; set; } = new();
    }

    public class JdRequirementDto
    {
        public string ReqId { get; set; } = string.Empty;
        public string RawText { get; set; } = string.Empty;
        public string NormalizedText { get; set; } = string.Empty;
        public string Importance { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public JdEntityDto Entities { get; set; } = new();
    }

    public class JdEntityDto
    {
        public string? SkillName { get; set; }
        public List<string> Alternatives { get; set; } = new();
        public int? YearsRequired { get; set; }
    }

    public class CvChunkDto
    {
        public string ChunkId { get; set; } = string.Empty;
        public string SectionType { get; set; } = string.Empty;
        public string RawText { get; set; } = string.Empty;
        public float[]? Embedding { get; set; }
    }

    public class RequirementScoreDto
    {
        public string ReqId { get; set; } = string.Empty;
        public string HandlerUsed { get; set; } = string.Empty;
        public decimal HandlerScore { get; set; }
        public string Reasoning { get; set; } = string.Empty;
        public string Confidence { get; set; } = string.Empty;
        public string? Flag { get; set; }
        public decimal AllocatedPoints { get; set; }
        public decimal ContributedPoints { get; set; }
    }

    public class PenaltyResultDto
    {
        public string PenaltyType { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public decimal DeductedPoints { get; set; }
    }
}
