using System;

namespace ITHunterview.Service.DTOs.Interview
{
    public class InterviewReportDto
    {
        public Guid Id { get; set; }
        public Guid SessionId { get; set; }
        public decimal? TotalScore { get; set; }
        public string OverallFeedback { get; set; }
    }
}
