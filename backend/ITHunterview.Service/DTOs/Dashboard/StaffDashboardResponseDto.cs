namespace ITHunterview.Service.DTOs.Dashboard;

public class StaffDashboardResponseDto
{
    public int TotalQuestions { get; set; }
    public int NewQuestions { get; set; }
    public int PendingCompanies { get; set; }
    public int AuditWarnings { get; set; }

    public List<QuestionCategoryDto> QuestionsByCategory { get; set; } = new();
    public List<QuestionLevelDto> QuestionsByLevel { get; set; } = new();
    public List<CompanyVerificationDto> CompanyVerifications { get; set; } = new();
}

public class QuestionCategoryDto
{
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
}

public class QuestionLevelDto
{
    public string Level { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class CompanyVerificationDto
{
    public string Week { get; set; } = string.Empty;
    public int New { get; set; }
    public int Verified { get; set; }
}
