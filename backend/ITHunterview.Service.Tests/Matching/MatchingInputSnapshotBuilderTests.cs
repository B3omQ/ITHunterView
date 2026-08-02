using System.Text.Json;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service.Matching;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingInputSnapshotBuilderTests
{
    [Fact]
    public async Task BuildAsync_SavedSourcesCopiesAuthorizedRawAndParsedData()
    {
        var userId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var cv = new Cvs
        {
            Id = cvId,
            UserId = userId,
            FileName = "trusted.pdf",
            RawText = "CV raw text",
            ParsedData = "{\"schema_version\":\"cv-analysis/v2\"}"
        };
        var job = new JobPostings
        {
            Id = jobId,
            Title = "Backend Engineer",
            Description = "Build APIs",
            Requirements = "C# and PostgreSQL",
            Benefits = "Flexible work",
            ParsedData = "{\"schema_version\":\"jd-analysis/v3\"}"
        };
        var repository = new Mock<IMatchingSourceRepository>();
        repository.Setup(x => x.GetOwnedCvAsync(cvId, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(cv);
        repository.Setup(x => x.GetAccessibleJobAsync(jobId, userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(job);
        var builder = new MatchingInputSnapshotBuilder(repository.Object);

        var result = await builder.BuildAsync(
            userId,
            new PreparedMatchingRequest(
                new PreparedSavedCvSource(cvId, "client-name.pdf"),
                new PreparedSavedJdSource(jobId, "client title"),
                MatchingMode.JdFit));

        result.Snapshot.Cv.FileName.Should().Be("trusted.pdf");
        result.Snapshot.Cv.OriginalText.Should().Be("CV raw text");
        result.Snapshot.Cv.AnalysisJson.Should().Be(cv.ParsedData);
        result.Snapshot.Cv.AnalysisSchemaVersion.Should().Be("cv-analysis/v2");
        result.Snapshot.Jd.Title.Should().Be("Backend Engineer");
        result.Snapshot.Jd.OriginalText.Should().Contain("Description: Build APIs");
        result.Snapshot.Jd.OriginalText.Should().Contain("Requirements: C# and PostgreSQL");
        result.Snapshot.Jd.AnalysisSchemaVersion.Should().Be("jd-analysis/v3");
        JsonDocument.Parse(result.Json).RootElement.GetProperty("cv").GetProperty("sourceId").GetGuid().Should().Be(cvId);
    }

    [Fact]
    public async Task BuildAsync_RawSourcesCopiesValidatedTextExactly()
    {
        var repository = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        var builder = new MatchingInputSnapshotBuilder(repository.Object);
        var cvText = "  " + new string('c', 100) + "  ";
        var jdText = "  " + new string('j', 100) + "  ";

        var result = await builder.BuildAsync(
            Guid.NewGuid(),
            new PreparedMatchingRequest(
                new PreparedRawCvSource(cvText, "paste.txt"),
                new PreparedRawJdSource(jdText, "Backend role"),
                MatchingMode.JdFit));

        result.Snapshot.Cv.OriginalText.Should().Be(cvText);
        result.Snapshot.Jd.OriginalText.Should().Be(jdText);
        result.Snapshot.Cv.SourceId.Should().BeNull();
        result.Snapshot.Jd.SourceId.Should().BeNull();
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildAsync_SerializesDetachedValuesWhenSourceChangesLater()
    {
        var userId = Guid.NewGuid();
        var cv = new Cvs
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FileName = "before.pdf",
            RawText = "before",
            ParsedData = "{\"schema_version\":\"cv-analysis/v2\"}"
        };
        var job = new JobPostings
        {
            Id = Guid.NewGuid(),
            Title = "Before",
            Description = "before description",
            Requirements = "before requirements",
            Benefits = "before benefits"
        };
        var repository = new Mock<IMatchingSourceRepository>();
        repository.Setup(x => x.GetOwnedCvAsync(cv.Id, userId, It.IsAny<CancellationToken>())).ReturnsAsync(cv);
        repository.Setup(x => x.GetAccessibleJobAsync(job.Id, userId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(job);
        var builder = new MatchingInputSnapshotBuilder(repository.Object);

        var result = await builder.BuildAsync(
            userId,
            new PreparedMatchingRequest(
                new PreparedSavedCvSource(cv.Id, cv.FileName),
                new PreparedSavedJdSource(job.Id, job.Title),
                MatchingMode.JdFit));
        cv.RawText = "after";
        cv.FileName = "after.pdf";
        job.Title = "After";
        job.Description = "after description";

        result.Json.Should().Contain("before").And.NotContain("after");
    }

    [Fact]
    public async Task BuildAsync_HashExcludesSubmissionTimestamp()
    {
        var userId = Guid.NewGuid();
        var repository = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        var builder = new MatchingInputSnapshotBuilder(repository.Object);
        var request = new PreparedMatchingRequest(
            new PreparedRawCvSource(new string('c', 100), "cv.txt"),
            new PreparedRawJdSource(new string('j', 100), "jd"),
            MatchingMode.JdFit);

        var first = await builder.BuildAsync(userId, request);
        await Task.Delay(10);
        var second = await builder.BuildAsync(userId, request);

        first.Snapshot.SubmittedAtUtc.Should().NotBe(second.Snapshot.SubmittedAtUtc);
        first.Sha256.Should().Be(second.Sha256);
        first.Sha256.Should().MatchRegex("^[0-9a-f]{64}$");
    }

    [Fact]
    public async Task BuildAsync_MaximumRawSourcesRemainValidJson()
    {
        var repository = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        var builder = new MatchingInputSnapshotBuilder(repository.Object);
        var result = await builder.BuildAsync(
            Guid.NewGuid(),
            new PreparedMatchingRequest(
                new PreparedRawCvSource(new string('c', 100_000), null),
                new PreparedRawJdSource(new string('j', 100_000), null),
                MatchingMode.JdFit));

        var document = JsonDocument.Parse(result.Json);
        document.RootElement.GetProperty("cv").GetProperty("originalText").GetString()!.Length.Should().Be(100_000);
        result.Json.Length.Should().BeGreaterThan(200_000);
    }
}
