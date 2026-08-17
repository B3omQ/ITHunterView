using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Domain.Entities;

[Table("recruiter_cv_scan_results")]
public sealed class RecruiterCvScanResult
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("run_id")]
    public Guid RunId { get; set; }

    [Column("cv_id")]
    public Guid CvId { get; set; }

    [Column("candidate_user_id")]
    public Guid CandidateUserId { get; set; }

    [Column("match_score")]
    public decimal? MatchScore { get; set; }

    [Column("match_details")]
    public string MatchDetails { get; set; } = string.Empty;

    [Column("cv_analysis_quality")]
    public CvAnalysisQuality? CvAnalysisQuality { get; set; }

    [Column("cv_analysis_coverage_json", TypeName = "jsonb")]
    public string? CvAnalysisCoverageJson { get; set; }

    [Column("cv_analysis_diagnostics_json", TypeName = "jsonb")]
    public string? CvAnalysisDiagnosticsJson { get; set; }

    [Column("rank")]
    public int Rank { get; set; }
}
