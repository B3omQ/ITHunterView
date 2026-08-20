using System;

namespace ITHunterview.Service.DTOs.Cv.Matching;

public sealed class UnlockCandidateRequestDto
{
    public Guid ScanResultId { get; set; }
    public Guid CvId { get; set; }
    public Guid? JobId { get; set; }
}

public sealed class UnlockCandidateResponseDto
{
    public Guid UnlockId { get; set; }
    public Guid ScanResultId { get; set; }
    public Guid CvId { get; set; }
    public Guid CandidateUserId { get; set; }
    public string CandidateName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public string UnlockedVia { get; set; } = string.Empty;
    public int CoinsSpent { get; set; }
    public DateTime UnlockedAt { get; set; }
    public bool IsRetainedCopy { get; set; }

    // Compatibility properties
    public bool Success { get; set; } = true;
    public string Message { get; set; } = string.Empty;
    public int CoinsDeducted { get; set; }
    public int RemainingCoins { get; set; }
    public Guid? CandidateId { get; set; }
    public string? CvFileName { get; set; }
}
