using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Domain.Entities;

[Table("candidate_job_scan_runs")]
public sealed class CandidateJobScanRun
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("candidate_user_id")]
    public Guid CandidateUserId { get; set; }

    [Column("cv_id")]
    public Guid CvId { get; set; }

    [Column("cv_file_name_snapshot")]
    public string CvFileNameSnapshot { get; set; } = string.Empty;

    [Column("status")]
    public MatchingScanRunStatus Status { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [Column("started_at")]
    public DateTime? StartedAt { get; set; }

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }

    [Column("error_code")]
    public string? ErrorCode { get; set; }

    [Column("error_message")]
    public string? ErrorMessage { get; set; }
}
