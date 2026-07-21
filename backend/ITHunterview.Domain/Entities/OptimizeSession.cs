using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using ITHunterview.Domain.Entities.Cv;

namespace ITHunterview.Domain.Entities;

public class OptimizeSession : BaseEntity
{
    public Guid MatchSessionId { get; set; }
    
    public string OriginalFileType { get; set; } = null!; // "pdf" or "docx"
    
    // In PostgreSQL, this can be mapped to JSONB
    [Column(TypeName = "jsonb")]
    public CvDocument? CvDocument { get; set; }
}
