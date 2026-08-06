using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Cv.Matching
{
    public class MatchingRequestDto
    {
        public Guid? CvId { get; set; }
        [Obsolete("Public CV URL sources are not supported. Use CvId or CvText.")]
        public string? CvUrl { get; set; }
        public string? CvText { get; set; }

        public Guid? JobId { get; set; }
        public string? RawJdText { get; set; }
        
        public string? CvFileName { get; set; }
        public string? JdTitle { get; set; }

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
