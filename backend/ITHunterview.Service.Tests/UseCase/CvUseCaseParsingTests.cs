using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Exceptions;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.UseCase;

public sealed class CvUseCaseParsingTests
{
    [Fact]
    public async Task ParseCvBackgroundAsync_PartialCanonicalJson_PersistsSuccessAndQualityMetadata()
    {
        var cv = CreateCv();
        var coverage = new CvAnalysisCoverage(2, 1, 1, 1, 1, 0, 0, 0, 0, true, true, false, true);
        var diagnostics = new[] { new CvAnalysisDiagnostic("INTEGER_INVALID", "$.matching_metrics.total_years_exp") };
        var canonical = CreateCanonical(CvAnalysisQuality.PARTIAL, coverage, diagnostics);
        var repository = new Mock<ICvRepository>();
        repository.Setup(x => x.GetByIdAsync(cv.Id)).ReturnsAsync(cv);
        var extractor = new Mock<ICvTextExtractorService>();
        extractor.Setup(x => x.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!)).ReturnsAsync(canonical);
        var sut = CreateUseCase(repository, extractor);

        await sut.ParseCvBackgroundAsync(cv.Id, cv.RawText!, cv.FileUrl);

        cv.ParseStatus.Should().Be("SUCCESS");
        cv.AnalysisQuality.Should().Be(CvAnalysisQuality.PARTIAL);
        cv.AnalysisCoverageJson.Should().NotBeNullOrWhiteSpace();
        cv.AnalysisDiagnosticsJson.Should().Contain("INTEGER_INVALID");
        cv.ParseError.Should().BeNull();
        repository.Verify(x => x.UpdateAsync(cv), Times.Once);
    }

    [Fact]
    public async Task ParseCvBackgroundAsync_InvalidOutput_PersistsBoundedFailureAndClearsStaleQuality()
    {
        var cv = CreateCv();
        cv.AnalysisQuality = CvAnalysisQuality.COMPLETE;
        cv.AnalysisCoverageJson = "{}";
        cv.AnalysisDiagnosticsJson = "[]";
        var repository = new Mock<ICvRepository>();
        repository.Setup(x => x.GetByIdAsync(cv.Id)).ReturnsAsync(cv);
        var extractor = new Mock<ICvTextExtractorService>();
        extractor.Setup(x => x.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!))
            .ThrowsAsync(new CvAnalysisValidationException(CvAnalysisValidationResult.Invalid(
                "CV_ANALYSIS_INVALID_JSON",
                "JSON_PARSE_FAILED",
                "$")));
        var sut = CreateUseCase(repository, extractor);

        await sut.ParseCvBackgroundAsync(cv.Id, cv.RawText!, cv.FileUrl);

        cv.ParseStatus.Should().Be("FAILED");
        cv.ParseError.Should().Be("CV_ANALYSIS_INVALID_JSON");
        cv.AnalysisQuality.Should().BeNull();
        cv.AnalysisCoverageJson.Should().BeNull();
        cv.AnalysisDiagnosticsJson.Should().BeNull();
    }

    private static CvUseCase CreateUseCase(Mock<ICvRepository> repository, Mock<ICvTextExtractorService> extractor)
    {
        var provider = new Mock<IServiceProvider>();
        provider.Setup(x => x.GetService(typeof(ICvTextExtractorService))).Returns(extractor.Object);
        provider.Setup(x => x.GetService(typeof(ICvRepository))).Returns(repository.Object);
        var scope = new Mock<IServiceScope>();
        scope.SetupGet(x => x.ServiceProvider).Returns(provider.Object);
        var scopeFactory = new Mock<IServiceScopeFactory>();
        scopeFactory.Setup(x => x.CreateScope()).Returns(scope.Object);
        return new CvUseCase(
            repository.Object,
            scopeFactory.Object,
            NullLogger<CvUseCase>.Instance,
            extractor.Object,
            Mock.Of<ICandidateProfileRepository>(),
            new MemoryCache(new MemoryCacheOptions()));
    }

    private static Cvs CreateCv() => new()
    {
        Id = Guid.NewGuid(),
        FileUrl = "https://cdn.example/cv.pdf",
        FileName = "cv.pdf",
        FileType = "pdf",
        ParsedData = string.Empty,
        ParseStatus = "PROCESSING",
        RawText = "Jane Doe\nBackend developer\n"
    };

    private static string CreateCanonical(
        CvAnalysisQuality quality,
        CvAnalysisCoverage coverage,
        IReadOnlyCollection<CvAnalysisDiagnostic> diagnostics) =>
        System.Text.Json.JsonSerializer.Serialize(new
        {
            schema_version = "cv-analysis/v2",
            analysis_quality = quality.ToString(),
            analysis_coverage = coverage,
            analysis_diagnostics = diagnostics,
            verbatim_sections = new { },
            matching_metrics = new { },
            matching_evidence = new { }
        }, new System.Text.Json.JsonSerializerOptions { PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.SnakeCaseLower });
}
