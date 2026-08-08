using System;
using System.Collections.Generic;

namespace ITHunterview.Service.DTOs.Optimize;

public class CreateOptimizeSessionRequest
{
    public string? CvUrl { get; set; }
    public Guid? CvId { get; set; }
}

public class SectionAnalysisDto
{
    public string SectionName { get; set; } = null!;
    public bool IsPresent { get; set; }
    public string Status { get; set; } = "Missing"; // "Good", "Warning", "Missing"
    public string Feedback { get; set; } = null!;
}

public class PriorityOrderCheckDto
{
    public string CandidateLevel { get; set; } = "Experienced"; // "Student/Fresher" or "Experienced"
    public bool IsOrderOptimal { get; set; }
    public string CurrentOrderDescription { get; set; } = null!;
    public string RecommendedOrderDescription { get; set; } = null!;
    public string Advice { get; set; } = null!;
}

public class CvImprovementRecommendationDto
{
    public string Category { get; set; } = null!; // "Structure", "Contact", "Experience", "Skills", "Formatting"
    public string Title { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Priority { get; set; } = "Medium"; // "High", "Medium", "Low"
    public string? ExampleBefore { get; set; }
    public string? ExampleAfter { get; set; }
}

public class CvOptimizationResultDto
{
    public Guid SessionId { get; set; }
    public Guid? CvId { get; set; }
    public string? CvFileName { get; set; }
    public double OverallScore { get; set; }
    public string Summary { get; set; } = null!;
    public List<SectionAnalysisDto> Sections { get; set; } = new();
    public PriorityOrderCheckDto PriorityOrder { get; set; } = new();
    public List<CvImprovementRecommendationDto> Recommendations { get; set; } = new();
}

public class OptimizeHistoryItemDto
{
    public Guid SessionId { get; set; }
    public Guid? CvId { get; set; }
    public string? CvFileName { get; set; }
    public string OriginalFileType { get; set; } = "pdf";
    public double OverallScore { get; set; }
    public DateTime CreatedAt { get; set; }
}
