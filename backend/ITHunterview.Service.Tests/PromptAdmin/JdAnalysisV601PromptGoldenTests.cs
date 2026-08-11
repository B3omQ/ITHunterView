using FluentAssertions;
using ITHunterview.Service.Constant.Prompts;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Service;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.PromptAdmin;

public sealed class JdAnalysisV601PromptGoldenTests
{
    private const string PreviousResponsibilityPolicy = """
        RESPONSIBILITY VERSUS REQUIREMENT

        Job duties are not automatically candidate requirements.

        Statements beginning with words such as:

        - develop
        - build
        - maintain
        - integrate
        - collaborate
        - participate
        - support
        - deliver
        - fix
        - review
        - manage

        normally describe responsibilities.

        Do not create a candidate requirement merely because a technology appears in a responsibility.

        Extract it only when the source explicitly presents it as:

        - a candidate qualification;
        - a prerequisite;
        - an expected capability;
        - a required skill;
        - a preferred capability;
        - or an experience requirement.

        For example:

        "Build and deliver new features using ReactJS and Laravel."

        is normally a responsibility and must not, by itself, create ReactJS and Laravel candidate requirements.

        But:

        "FE: Proficient in ReactJS. BE: Proficient in PHP - Laravel."

        explicitly states candidate qualifications and must create the corresponding requirements.
        """;

    private const string ApprovedResponsibilityPolicy = """
        RESPONSIBILITY VERSUS REQUIREMENT

        Job duties remain role responsibilities and are not automatically candidate requirements.

        Extract a responsibility-derived capability only when the complete clause names a concrete and independently assessable technical, leadership, operational, architecture, quality, security, performance, scalability, deployment, or delivery capability.

        Examples of assessable capabilities include designing microservices, making architecture decisions, implementing CI/CD, performing code review, optimizing system performance, or leading and mentoring a stated engineering team.

        Do not extract generic activity alone, including generic collaboration, participation, support, communication, delivery, maintenance, or attendance without a concrete assessable capability.

        A capability derived only from responsibility wording is nice_to_have. It is must_have only when the same complete source clause contains explicit mandatory language. Preserve the physical source_section as description and preserve the complete clause verbatim.

        When the same capability also appears as an explicit candidate qualification, use the explicit qualification occurrence and do not duplicate the responsibility occurrence.
        """;

    private const string ExistingAlternativesRule = "Keep every explicit alternative from one clause in one one_of group. Downstream display keeps that group on one line and separates alternatives with \" | \". Do not split those alternatives into independent required rows.";
    private const string ApprovedOneOfMinimumRule = "A one_of group must contain at least two distinct explicit alternatives from the source clause. If a group contains only one independently assessable item, use all_of. Example lists and aliases do not satisfy this minimum.";

    [Fact]
    public void V601SystemFixture_DiffFromV600IsExactlyTheTwoApprovedSemanticChanges()
    {
        var previous = NormalizeLineEndings(ReadFixture("jd-analysis-v6-system-semantic.txt"));
        var actual = NormalizeLineEndings(ReadFixture("jd-analysis-v6.0.1-system-semantic.txt"));

        Count(previous, PreviousResponsibilityPolicy).Should().Be(1);
        Count(previous, ExistingAlternativesRule).Should().Be(1);
        var expected = previous
            .Replace(PreviousResponsibilityPolicy, ApprovedResponsibilityPolicy, StringComparison.Ordinal)
            .Replace(
                ExistingAlternativesRule,
                ExistingAlternativesRule + "\n\n" + ApprovedOneOfMinimumRule,
                StringComparison.Ordinal);

        actual.Should().Be(expected);
    }

    [Fact]
    public void V601UserFixture_IsByteEquivalentToV600AfterCanonicalLfNormalization()
    {
        var previous = NormalizeLineEndings(ReadFixture("jd-analysis-v6-user-semantic.txt"));
        var actual = NormalizeLineEndings(ReadFixture("jd-analysis-v6.0.1-user-semantic.txt"));

        actual.Should().Be(previous);
    }

    [Fact]
    public void V601SemanticFixtures_AreSchemaFreeAndDeclareApprovedRules()
    {
        var system = ReadFixture("jd-analysis-v6.0.1-system-semantic.txt");
        var user = ReadFixture("jd-analysis-v6.0.1-user-semantic.txt");

        system.Should().Contain(ApprovedResponsibilityPolicy);
        system.Should().Contain(ApprovedOneOfMinimumRule);
        system.Should().NotContain(JdAnalysisOutputSchema.BeginMarker);
        system.Should().NotContain(JdAnalysisOutputSchema.EndMarker);
        system.Should().NotContain("\"schema_version\"");
        system.Should().NotContain("\"requirement_groups\"");
        user.Should().NotContain(JdAnalysisOutputSchema.BeginMarker);
        user.Should().NotContain("\"schema_version\"");
    }

    [Fact]
    public void V601Composition_AppendsExactlyOneUnchangedLockedV5Schema()
    {
        var semantic = ReadFixture("jd-analysis-v6.0.1-system-semantic.txt");
        var composed = JdAnalysisOutputSchema.ComposeSystemPrompt(semantic);

        Count(composed, JdAnalysisOutputSchema.BeginMarker).Should().Be(1);
        Count(composed, JdAnalysisOutputSchema.EndMarker).Should().Be(1);
        composed.Should().EndWith(JdAnalysisOutputSchema.LockedBlock.Trim());
        composed.Should().Contain("\"schema_version\": \"jd-analysis/v5\"");
    }

    [Fact]
    public void CannedV5Output_PreservesDescriptionCapabilityAndRequirementsQualificationMechanically()
    {
        const string descriptionClause = "Design microservices and optimize system performance.";
        const string requirementClause = "Proficient in ReactJS.";
        var input = new JobAnalysisInputSnapshot
        {
            Description = descriptionClause,
            Requirements = requirementClause
        };
        const string providerOutput = """
            {
              "schema_version": "jd-analysis/v5",
              "matching_metrics": {
                "job_titles_normalized": [],
                "total_years_exp": 0,
                "domains": [],
                "requirement_groups": [
                  {
                    "source_requirement_id": "req-001",
                    "intent": "qualification",
                    "operator": "all_of",
                    "importance": "nice_to_have",
                    "source_section": "description",
                    "requirement_verbatim": "Design microservices and optimize system performance.",
                    "items": [
                      {
                        "category": "tech_skill",
                        "skill_name": "microservices design",
                        "raw_mention": "Design microservices"
                      }
                    ]
                  },
                  {
                    "source_requirement_id": "req-002",
                    "intent": "qualification",
                    "operator": "all_of",
                    "importance": "must_have",
                    "source_section": "requirements",
                    "requirement_verbatim": "Proficient in ReactJS.",
                    "items": [
                      {
                        "category": "tech_skill",
                        "skill_name": "react",
                        "raw_mention": "ReactJS"
                      }
                    ]
                  }
                ]
              }
            }
            """;

        var validation = new JdAnalysisResponseValidator().Validate(providerOutput, input);
        validation.IsUsable.Should().BeTrue(validation.FailureCode);
        validation.Data.Should().NotBeNull();

        var extractionService = new JobAnalysisExtractionService(
            Mock.Of<IAiService>(),
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IJdAnalysisResponseValidator>(),
            NullLogger<JobAnalysisExtractionService>.Instance);
        var effectiveJson = extractionService.SerializeEffectiveAnalysis(validation.Data!);
        var projection = new JdRequirementProjector().Project(effectiveJson);

        projection.Groups.Should().HaveCount(2);
        projection.Groups.Select(group => group.SourceSection)
            .Should().Equal("description", "requirements");
        projection.Groups.Select(group => group.RequirementVerbatim)
            .Should().Equal(descriptionClause, requirementClause);
    }

    private static string ReadFixture(string name) => File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "PromptAdmin", "Fixtures", name));

    private static string NormalizeLineEndings(string value) =>
        value.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');

    private static int Count(string content, string value) =>
        content.Split(value, StringSplitOptions.None).Length - 1;
}
