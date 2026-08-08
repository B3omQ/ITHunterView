using FluentAssertions;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class CvJdOneToOneMatchingProcessorTests
{
    [Fact]
    public async Task ExecuteAsync_ForwardsImmutableSnapshotAndCancellationToEngine()
    {
        var matchId = Guid.NewGuid();
        var cancellation = new CancellationTokenSource().Token;
        var snapshot = new MatchingInputSnapshotV1(
            MatchingInputSnapshotBuilder.SchemaVersion,
            MatchingMode.JdFit,
            new MatchingCvSnapshot("raw_cv", null, "cv.pdf", new string('c', 100), null, null),
            new MatchingJdSnapshot("raw_jd", null, "JD", new string('j', 100), null, null),
            DateTime.UtcNow);
        var expected = new CvJdMatchingExecutionResult(0.75m, "{\"score\":0.75}", null);
        var engine = new Mock<ICvJdOneToOneMatchingEngine>();
        engine.Setup(x => x.ExecuteAsync(matchId, snapshot, cancellation)).ReturnsAsync(expected);
        var processor = new CvJdOneToOneMatchingProcessor(engine.Object);

        var actual = await processor.ExecuteAsync(matchId, snapshot, cancellation);

        actual.Should().Be(expected);
        engine.Verify(x => x.ExecuteAsync(matchId, snapshot, cancellation), Times.Once);
    }
}
