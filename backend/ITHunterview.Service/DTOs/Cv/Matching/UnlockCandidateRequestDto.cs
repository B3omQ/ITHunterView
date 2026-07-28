using System;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class UnlockCandidateRequestDto
    {
        public Guid CvId { get; set; }
        public Guid? JobId { get; set; }
    }

    public class UnlockCandidateResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UnlockedVia { get; set; } = string.Empty; // "SUBSCRIPTION" or "COINS"
        public int CoinsDeducted { get; set; }
        public int RemainingCoins { get; set; }
        public Guid? CvId { get; set; }
        public Guid? CandidateId { get; set; }
        public string? CvFileName { get; set; }
        public string? FileUrl { get; set; }
    }
}
