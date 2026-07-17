using System.Text.Json.Serialization;

namespace ITHunterview.Domain.Entities.Cv;

public record CvDocument
{
    public required CvHeader Header { get; init; }
    public string? Summary { get; init; }
    public List<CvExperience> Experience { get; init; } = [];
    public List<string> Skills { get; init; } = [];
    public List<CvEducation> Education { get; init; } = [];
}

public record CvHeader
{
    public required string FullName { get; init; }
    public string? Title { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
}

public record CvExperience
{
    public required string Company { get; init; }
    public required string Role { get; init; }
    public string? DateRange { get; init; }
    public List<string> Bullets { get; init; } = [];
}

public record CvEducation
{
    public required string School { get; init; }
    public string? Degree { get; init; }
    public string? DateRange { get; init; }
}
