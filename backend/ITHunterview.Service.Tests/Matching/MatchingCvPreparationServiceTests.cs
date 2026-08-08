using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Config;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Options;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingCvPreparationServiceTests
{
    [Fact]
    public async Task PrepareAsync_V2SavedCvWithUsableAnalysis_ReusesCanonicalDataWithoutExtraction()
    {
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(value => value.ValidateAndCanonicalize("stored-cv-analysis"))
            .Returns(new CvAnalysisValidationResult(
                CvAnalysisQuality.COMPLETE,
                "canonical-cv-analysis",
                null,
                Array.Empty<CvAnalysisDiagnostic>(),
                string.Empty));
        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        var sources = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        var service = new MatchingCvPreparationService(
            validator.Object,
            extractor.Object,
            sources.Object,
            Options.Create(new CloudinarySettings { CloudName = "demo" }));
        var snapshot = new MatchingInputSnapshotV1(
            MatchingInputSnapshotBuilder.SchemaVersion,
            MatchingMode.JdFit,
            new MatchingCvSnapshot(
                "saved_cv",
                Guid.NewGuid(),
                "resume.pdf",
                "raw CV",
                "stored-cv-analysis",
                "cv-analysis/v2",
                "https://res.cloudinary.com/demo/raw/upload/v1/cv/resume.pdf",
                "source-hash",
                "SUCCESS"),
            new MatchingJdSnapshot("raw_jd", null, null, "JD", null, null),
            DateTime.UtcNow);

        var result = await service.PrepareAsync(Guid.NewGuid(), snapshot);

        result.CanonicalJson.Should().Be("canonical-cv-analysis");
        result.Quality.Should().Be(CvAnalysisQuality.COMPLETE);
        result.PersistenceIntent.Should().BeNull();
        extractor.VerifyNoOtherCalls();
        sources.VerifyNoOtherCalls();
    }
}
