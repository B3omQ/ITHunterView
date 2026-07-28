using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
using Moq;

namespace ITHunterview.Service.Tests.JobAnalysis;

public class JobAnalysisEffectiveAnalysisTests
{
    [Fact]
    public void SerializeEffectiveAnalysis_WhenDictionarySkillsAreFiltered_PreservesEveryDetailedRequirement()
    {
        var service = new JobAnalysisExtractionService(
            Mock.Of<IAiService>(),
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IJdAnalysisResponseValidator>());

        var analysis = new ValidatedJobAnalysis
        {
            JobTitlesNormalized = new List<string> { "business analyst" },
            SkillsNormalized = new List<ValidatedSkillMention>
            {
                new() { Name = "jira", Category = "tech_skill", Importance = "must_have" },
                new() { Name = "kubernetes", Category = "tech_skill", Importance = "nice_to_have" }
            },
            RequirementsList = new List<ValidatedRequirementItem>
            {
                new() { SkillName = "Jira", Category = "tech_skill", Importance = "must_have" },
                new() { SkillName = "Kubernetes", Category = "tech_skill", Importance = "nice_to_have" },
                new() { SkillName = "3 years experience", Category = "experience", Importance = "must_have" }
            }
        };

        string json = service.SerializeEffectiveAnalysis(analysis, new HashSet<string> { "jira" });
        using var document = JsonDocument.Parse(json);
        var metrics = document.RootElement.GetProperty("matching_metrics");

        var skills = metrics.GetProperty("skills_normalized").EnumerateArray().ToList();
        var requirements = metrics.GetProperty("requirements_list").EnumerateArray().ToList();

        Assert.Single(skills);
        Assert.Equal("jira", skills[0].GetProperty("name").GetString());
        Assert.Equal(3, requirements.Count);
        Assert.Contains(requirements, item => item.GetProperty("skill_name").GetString() == "Kubernetes");
        Assert.Contains(requirements, item => item.GetProperty("skill_name").GetString() == "3 years experience");
    }
}
