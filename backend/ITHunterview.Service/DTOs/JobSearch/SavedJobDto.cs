using System;

namespace ITHunterview.Service.DTOs.JobSearch
{
    public class SavedJobDto
    {
        public Guid JobId { get; set; }
        public string Title { get; set; }
        public string CompanyName { get; set; }
        public string LogoUrl { get; set; }
        public string Location { get; set; }
        public string SalaryText { get; set; }
        public DateTime SavedAt { get; set; }
    }
}
