namespace ITHunterview.Service.DTOs.Dashboard;

public class RecruiterDashboardResponseDto
{
    public int ActiveJobs { get; set; }
    public int TotalApplications { get; set; }

    public List<DailyApplicationDto> DailyApplications { get; set; } = new();
    public List<ApplicationStatusDto> ApplicationStatus { get; set; } = new();
    public List<TopJobDto> TopJobs { get; set; } = new();
}

public class DailyApplicationDto
{
    public string Day { get; set; } = string.Empty;
    public int Apps { get; set; }
}

public class ApplicationStatusDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class TopJobDto
{
    public string Title { get; set; } = string.Empty;
    public int Applicants { get; set; }
}
