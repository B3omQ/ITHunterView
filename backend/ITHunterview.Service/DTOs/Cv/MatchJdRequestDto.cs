using System;

namespace ITHunterview.Service.DTOs.Cv
{
    public class MatchJdRequestDto
    {
        public Guid? CvId { get; set; }
        public string? CvUrl { get; set; }
        public string? CvText { get; set; }
        public Guid? JobId { get; set; }
        public string? RawJdText { get; set; }
    }
}
