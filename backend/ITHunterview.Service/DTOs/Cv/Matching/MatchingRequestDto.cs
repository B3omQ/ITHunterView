using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class MatchingRequestDto
    {
        public Guid? CvId { get; set; }
        public string? CvUrl { get; set; }
        public string? CvText { get; set; }

        public Guid? JobId { get; set; }
        public string? RawJdText { get; set; }

        // Removed ModelName and ApiKey as they are read from appsettings now

        public MatchingMode Mode { get; set; } = MatchingMode.JdFit;
    }

    public enum MatchingMode
    {
        JdFit,
        CvQuality,
        Both
    }
}
