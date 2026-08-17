using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ITHunterview.Domain.Enums;

namespace ITHunterview.Domain.Entities;

[Table("recruiter_cv_scan_runs")]
public sealed class RecruiterCvScanRun
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("recruiter_user_id")]
    public Guid RecruiterUserId { get; set; }

    [Column("recruiter_profile_id")]
    public Guid RecruiterProfileId { get; set; }

    [Column("company_id")]
    public Guid CompanyId { get; set; }

    [Column("job_id")]
    public Guid JobId { get; set; }

    [Column("job_title_snapshot")]
    public string JobTitleSnapshot { get; set; } = string.Empty;

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
