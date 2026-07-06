using System.ComponentModel.DataAnnotations;

namespace ITHunterview.Service.DTOs.CandidateProfile
{
    public class AboutMeUpdateRequestDto
    {
        [MaxLength(500)]
        public string? AboutMe { get; set; }
    }
}
