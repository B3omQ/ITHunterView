using System;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.UseCase;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Interface.UseCase;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Service;
using ITHunterview.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Pgvector;
using Xunit;
using FluentAssertions;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Utils;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CvJobMatchingUseCaseTests
    {
        private readonly Mock<IAiEmbeddingService> _mockAiService;
        private readonly Mock<ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService> _mockExtractorService;
        private readonly Mock<IPromptManagementService> _mockPromptService;
        private readonly CvJobMatchingUseCase _sut;

        public CvJobMatchingUseCaseTests()
        {
            _mockAiService = new Mock<IAiEmbeddingService>();
            _mockExtractorService = new Mock<ITHunterview.Service.Interface.Service.Matching.ICvTextExtractorService>();
            _mockPromptService = new Mock<IPromptManagementService>();
            var mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger<CvJobMatchingUseCase>>();
            var mockTextAiService = new Mock<IAiService>();
            var mockFeatureUsageUseCase = new Mock<ITHunterview.Service.Interface.UseCase.ICandidateFeatureUsageUseCase>();
            var mockMatchingPreflightUseCase = new Mock<ITHunterview.Service.Interface.UseCase.IMatchingInputPreflightUseCase>();
            var mockMatchingSourceRepository = new Mock<ITHunterview.Service.Interface.Persistence.IMatchingSourceRepository>();
            var mockCvAnalysisResponseValidator = new Mock<ITHunterview.Service.Interface.Service.Matching.ICvAnalysisResponseValidator>();
            
            // Pass null for context since we only test methods that don't hit DB
            _sut = new CvJobMatchingUseCase(null!, _mockAiService.Object, _mockExtractorService.Object, mockLogger.Object, _mockPromptService.Object, mockTextAiService.Object, mockFeatureUsageUseCase.Object, mockMatchingPreflightUseCase.Object, mockMatchingSourceRepository.Object, mockCvAnalysisResponseValidator.Object);
        }

        [Theory]
        [InlineData("{\"skills\": \"C#, SQL\"}", "skills", "C#, SQL")]
        [InlineData("{\"position\": {\"title\": \"Backend Dev\"}}", "position.title", "Backend Dev")]
        [InlineData("{\"invalid\": json", "skills", "")]
        [InlineData(null, "skills", "")]
        [InlineData("", "skills", "")]
        [InlineData("{\"position\": {\"company\": \"XYZ\"}}", "position.title", "")]
        public void ExtractJsonField_ShouldReturnCorrectValue_BasedOnPath(string? json, string path, string expected)
        {
            // Act
            var result = _sut.ExtractJsonField(json, path);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractJsonField_ShouldExtractComplexArrayAsString()
        {
            // Arrange
            var json = @"{
                ""tech_requirements"": {
                    ""must_have"": [
                        { ""skill"": ""Java"" },
                        { ""skill"": ""Spring"" }
                    ]
                }
            }";

            // Act
            var result = _sut.ExtractJsonField(json, "tech_requirements.must_have");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("Java");
            result.Should().Contain("Spring");
        }

        [Fact]
        public void CalculateComponentScore_ShouldReturnZero_WhenVectorsAreNull()
        {
            // Act
            var score1 = _sut.CalculateComponentScore(null, new Vector(new float[] { 1, 0, 0 }));
            var score2 = _sut.CalculateComponentScore(new Vector(new float[] { 1, 0, 0 }), null);
            var score3 = _sut.CalculateComponentScore(null, null);

            // Assert
            score1.Should().Be(0m);
            score2.Should().Be(0m);
            score3.Should().Be(0m);
        }

        [Fact]
        public async Task GetMatchingResultAsync_MapsPartialCvAnalysisWithoutChangingCompletedStatus()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);
            var matchId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            var coverage = new CvAnalysisCoverage(
                2, 1, 1,
                3, 2, 1,
                1, 1, 0,
                true, true, true, false);
            context.CvJobMatchScores.Add(new CvJobMatchScores
            {
                Id = matchId,
                UserId = userId,
                Status = "Completed",
                MatchScore = .75m,
                MatchDetails = "{\"scoreBasis\":\"available_cv_metrics\"}",
                CvAnalysisQuality = CvAnalysisQuality.PARTIAL,
                CvAnalysisCoverageJson = CvAnalysisMetadataReader.SerializeCoverage(coverage),
                CvAnalysisDiagnosticsJson = CvAnalysisMetadataReader.SerializeDiagnostics(
                    [new CvAnalysisDiagnostic("DOMAIN_METRIC_MISSING", "$.matching_metrics.domains")]),
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var sut = CreateDatabaseUseCase(context);
            var result = await sut.GetMatchingResultAsync(matchId, userId);

            result.Should().NotBeNull();
            result!.Status.Should().Be("Completed");
            result.CvAnalysis.Should().NotBeNull();
            result.CvAnalysis!.Quality.Should().Be("PARTIAL");
            result.CvAnalysis.ScoreBasis.Should().Be("available_cv_metrics");
            result.CvAnalysis.WarningCodes.Should().Equal("DOMAIN_METRIC_MISSING");
        }

        [Fact]
        public async Task ProcessMatchingJobAsync_WhenBothParsersNeedScopedServices_DoesNotStartThemConcurrently()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);
            var matchId = Guid.NewGuid();
            var userId = Guid.NewGuid();
            context.CvJobMatchScores.Add(new CvJobMatchScores
            {
                Id = matchId,
                UserId = userId,
                Status = "Pending",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var cvCompletion = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            var jdStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            var extractor = new Mock<ICvTextExtractorService>();
            extractor
                .Setup(x => x.ExtractParsedDataFromRawTextAsync("raw cv", "pasted_text", null))
                .Returns(cvCompletion.Task);

            var jobExtraction = new Mock<IJobAnalysisExtractionService>();
            jobExtraction
                .Setup(x => x.ExtractWithActivePromptsAsync(It.IsAny<ITHunterview.Service.Utils.JobAnalysisInputSnapshot>(), default))
                .Returns(() =>
                {
                    jdStarted.TrySetResult();
                    return Task.FromException<JobAnalysisExtractionResult>(new InvalidOperationException("STOP_AFTER_ORDER_CHECK"));
                });

            var preflight = new Mock<IMatchingInputPreflightUseCase>();
            preflight
                .Setup(x => x.RecheckAccessAsync(userId, It.IsAny<PreparedMatchingRequest>(), default))
                .Returns(Task.CompletedTask);
            var featureUsage = new Mock<ICandidateFeatureUsageUseCase>();
            featureUsage
                .Setup(x => x.RefundFeatureUsageByReferenceAsync(userId, matchId, It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            var cvValidator = new Mock<ICvAnalysisResponseValidator>();
            cvValidator
                .Setup(x => x.ValidateAndCanonicalize("{}"))
                .Returns(CvAnalysisValidationResult.Complete(
                    "{}",
                    new CvAnalysisCoverage(
                        0, 0, 0,
                        0, 0, 0,
                        0, 0, 0,
                        true, true, true, true)));

            var sut = new CvJobMatchingUseCase(
                context,
                Mock.Of<IAiEmbeddingService>(),
                extractor.Object,
                NullLogger<CvJobMatchingUseCase>.Instance,
                Mock.Of<IPromptManagementService>(),
                Mock.Of<IAiService>(),
                featureUsage.Object,
                preflight.Object,
                Mock.Of<IMatchingSourceRepository>(),
                cvValidator.Object,
                jobExtraction.Object);
            var request = new PreparedMatchingRequest(
                new PreparedRawCvSource("raw cv", null),
                new PreparedRawJdSource("raw jd", null),
                MatchingMode.Both);

            var processing = sut.ProcessMatchingJobAsync(matchId, userId, request);
            await Task.Yield();

            jdStarted.Task.IsCompleted.Should().BeFalse("the two parsers share scoped EF-backed services");

            cvCompletion.SetResult("{}");
            await processing;
            jdStarted.Task.IsCompleted.Should().BeTrue();
        }

        private sealed class MatchingTestContext : ITHunterviewContext
        {
            public MatchingTestContext(DbContextOptions<ITHunterviewContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                modelBuilder.Entity<Cvs>().Ignore(x => x.TitleEmbedding);
                modelBuilder.Entity<Cvs>().Ignore(x => x.SkillsEmbedding);
                modelBuilder.Entity<Cvs>().Ignore(x => x.ExperienceEmbedding);
                modelBuilder.Entity<Cvs>().Ignore(x => x.DomainEmbedding);
                modelBuilder.Entity<JobPostings>().Ignore(x => x.TitleEmbedding);
                modelBuilder.Entity<JobPostings>().Ignore(x => x.SkillsEmbedding);
                modelBuilder.Entity<JobPostings>().Ignore(x => x.ExperienceEmbedding);
                modelBuilder.Entity<JobPostings>().Ignore(x => x.DomainEmbedding);
                modelBuilder.Entity<OptimizeSession>().Ignore(x => x.CvDocument);
            }
        }

        private static CvJobMatchingUseCase CreateDatabaseUseCase(ITHunterviewContext context) => new(
            context,
            Mock.Of<IAiEmbeddingService>(),
            Mock.Of<ICvTextExtractorService>(),
            NullLogger<CvJobMatchingUseCase>.Instance,
            Mock.Of<IPromptManagementService>(),
            Mock.Of<IAiService>(),
            Mock.Of<ICandidateFeatureUsageUseCase>(),
            Mock.Of<IMatchingInputPreflightUseCase>(),
            Mock.Of<IMatchingSourceRepository>(),
            Mock.Of<ICvAnalysisResponseValidator>());
    }
}
