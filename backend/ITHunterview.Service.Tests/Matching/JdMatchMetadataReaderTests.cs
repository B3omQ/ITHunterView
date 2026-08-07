using System.Text.Json;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Tests.Matching;

public sealed class JdMatchMetadataReaderTests
{
    [Fact]
    public void Read_AcceptsCoverageRecordWithPascalCaseProperties()
    {
        const string details = """
            {
              "jdAnalysis": {
                "quality": "PARTIAL",
                "scoreBasis": "accepted_requirements_only",
                "requirementSetComplete": false,
                "coverage": {
                  "InputGroupCount": 4,
                  "AcceptedGroupCount": 3,
                  "DiscardedGroupCount": 1,
                  "InputItemCount": 7,
                  "AcceptedItemCount": 5,
                  "DiscardedItemCount": 2,
                  "RequirementSetComplete": false
                },
                "warningCodes": ["INVALID_REQUIREMENT_GROUP"]
              }
            }
            """;

        var result = JdMatchMetadataReader.Read(details);

        Assert.NotNull(result);
        Assert.Equal("PARTIAL", result!.Quality);
        Assert.False(result.RequirementSetComplete);
        Assert.NotNull(result.Coverage);
        Assert.Equal(4, result.Coverage!.InputGroupCount);
        Assert.Equal(3, result.Coverage.AcceptedGroupCount);
        Assert.Equal(5, result.Coverage.AcceptedItemCount);
        Assert.False(result.Coverage.RequirementSetComplete);
        Assert.Equal("INVALID_REQUIREMENT_GROUP", Assert.Single(result.WarningCodes));
    }

    [Fact]
    public void Read_ParsesCalculatorOutputWithoutChangingQualityMetadata()
    {
        var details = JsonSerializer.Serialize(new
        {
            jdAnalysis = new
            {
                quality = "PARTIAL",
                scoreBasis = "complete_requirement_set",
                requirementSetComplete = true,
                coverage = new
                {
                    InputGroupCount = 1,
                    AcceptedGroupCount = 1,
                    DiscardedGroupCount = 0,
                    InputItemCount = 1,
                    AcceptedItemCount = 1,
                    DiscardedItemCount = 0,
                    RequirementSetComplete = true
                },
                warningCodes = new[] { "DUPLICATE_REQUIREMENT_GROUP" }
            }
        });

        var result = JdMatchMetadataReader.Read(details);

        Assert.NotNull(result);
        Assert.Equal("PARTIAL", result!.Quality);
        Assert.True(result.RequirementSetComplete);
        Assert.True(result.Coverage!.RequirementSetComplete);
        Assert.Equal("DUPLICATE_REQUIREMENT_GROUP", Assert.Single(result.WarningCodes));
    }
}
