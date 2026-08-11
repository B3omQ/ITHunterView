using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ITHunterview.Domain.Entities.Cv;

namespace ITHunterview.Domain.Entities;

public class OptimizeSession : BaseEntity
{
    public Guid? MatchSessionId { get; set; }
    public Guid? UserId { get; set; }
    public Guid? CvId { get; set; }
    public string? CvFileName { get; set; }
    
    public string OriginalFileType { get; set; } = "pdf"; // "pdf" or "docx"
    
    // In PostgreSQL, this can be mapped to JSONB
    [Column(TypeName = "jsonb")]
    public CvDocument? CvDocument { get; set; }

    public string? AnalysisResultJson { get; set; }
    public double? OverallScore { get; set; }
}
