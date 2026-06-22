using System;

namespace ITHunterview.Service.DTOs.JobApplication
{
    public class CreateJobApplicationRequestDto
    {
        public Guid JobId { get; set; }
        public Guid? CvId { get; set; }
        public string? CoverLetter { get; set; }
    }
}
