using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class ProfileCompletionStatusResponseDto
    {
        public bool IsComplete { get; set; }
        public List<string> MissingFields { get; set; } = new List<string>();
        public int CompletionPercentage { get; set; }
    }
}
