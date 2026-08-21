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
        public async Task MatchCvWithAllJobsAsync_PastedJdHistory_DoesNotAbortSavedJobMatching()
        {
            await using var context = CreateBulkMatchingContext();
            var (cv, job, user, profile) = CreateBulkMatchingEntities();
            var pastedJdMatch = new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = cv.UserId,
                CvId = cv.Id,
                JobId = null,
                RawJdText = "Pasted job description",
                MatchScore = 73m,
                MatchDetails = "pasted-jd-result",
                Status = "Completed",
                MatchType = "AI",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.CandidateProfiles.Add(profile);
            context.Cvs.Add(cv);
            context.JobPostings.Add(job);
            context.CvJobMatchScores.Add(pastedJdMatch);
            await context.SaveChangesAsync();

            await CreateBulkMatchingUseCase(context, cv.ParsedData!)
                .MatchCvWithAllJobsAsync(cv.Id, cv.UserId);

            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().HaveCount(2);
            scores.Single(score => score.JobId == job.Id).Status.Should().Be("Completed");
            var preservedPastedJdMatch = scores.Single(score => score.Id == pastedJdMatch.Id);
            preservedPastedJdMatch.JobId.Should().BeNull();
            preservedPastedJdMatch.MatchScore.Should().Be(73m);
            preservedPastedJdMatch.MatchDetails.Should().Be("pasted-jd-result");
        }

        [Fact]
        public async Task MatchJobWithAllCvsAsync_PastedCvHistory_DoesNotAbortSavedCvMatching()
        {
            await using var context = CreateBulkMatchingContext();
            var (cv, job, user, profile) = CreateBulkMatchingEntities();
            var pastedCvMatch = new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = Guid.NewGuid(),
                CvId = null,
                JobId = job.Id,
                RawJdText = job.Requirements,
                MatchScore = 64m,
                MatchDetails = "pasted-cv-result",
                Status = "Completed",
                MatchType = "AI",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            context.Users.Add(user);
            context.CandidateProfiles.Add(profile);
            context.Cvs.Add(cv);
            context.JobPostings.Add(job);
            context.CvJobMatchScores.Add(pastedCvMatch);
            await context.SaveChangesAsync();

            await CreateBulkMatchingUseCase(context, cv.ParsedData!)
                .MatchJobWithAllCvsAsync(job.Id, job.RecruiterId);

            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().HaveCount(2);
            scores.Single(score => score.CvId == cv.Id).Status.Should().Be("Completed");
            var preservedPastedCvMatch = scores.Single(score => score.Id == pastedCvMatch.Id);
            preservedPastedCvMatch.CvId.Should().BeNull();
            preservedPastedCvMatch.MatchScore.Should().Be(64m);
            preservedPastedCvMatch.MatchDetails.Should().Be("pasted-cv-result");
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
            result.ProcessingStage.Should().Be(MatchingProcessingStages.Completed);
            result.CvAnalysis.Should().NotBeNull();
            result.CvAnalysis!.Quality.Should().Be("PARTIAL");
            result.CvAnalysis.ScoreBasis.Should().Be("available_cv_metrics");
            result.CvAnalysis.WarningCodes.Should().Equal("DOMAIN_METRIC_MISSING");
        }

        [Fact]
        public async Task GetMatchingResultAsync_CompletedMalformedDetails_ReturnsTypedLegacySummary()
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
                Status = "Completed",
                MatchType = "AI",
                MatchScore = 81.8m,
                MatchDetails = "{not-json",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var result = await CreateDatabaseUseCase(context).GetMatchingResultAsync(matchId, userId);

            result.Should().NotBeNull();
            result!.Report.Should().NotBeNull();
            result.ReportKind.Should().Be("legacy_summary");
            result.MatchMethod.Should().Be("legacy_unknown");
            result.ScorePercent.Should().Be(81.8m);
            result.ScoreAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task GetMatchingResultAsync_V4_ReturnsTypedReportWithoutRawMatchDetails()
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
                Status = "Completed",
                MatchType = "AI",
                MatchScore = 91m,
                MatchDetails = "{\"contract\":\"jd-matching/v4\",\"jdFit\":{\"scorePercent\":91,\"requirementGroups\":[],\"criticalGaps\":[]}}",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var result = await CreateDatabaseUseCase(context).GetMatchingResultAsync(matchId, userId);

            result.Should().NotBeNull();
            result!.MatchDetails.Should().BeNull();
            result.Report.Should().NotBeNull();
            result.Report!.ReportContract.Should().Be("match-report/v3");
            result.ScorePercent.Should().Be(91m);
            result.ScoreAvailable.Should().BeTrue();
        }

        [Fact]
        public async Task GetMatchHistoryAsync_HardcodeFraction_ExposesNormalizedPercentAndMethod()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);
            var userId = Guid.NewGuid();
            context.CvJobMatchScores.Add(new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "Completed",
                MatchType = "Hardcode",
                ProductScope = ITHunterview.Domain.Enums.CvJobMatchProductScope.CandidateOneToOne,
                MatchScore = 0.818m,
                MatchDetails = "{\"Method\":\"HardcodeV3\",\"FinalScore\":0.818}",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var history = await CreateDatabaseUseCase(context).GetMatchHistoryAsync(userId, 1, 10);

            history.Items.Should().ContainSingle();
            history.Items[0].ScorePercent.Should().Be(81.8m);
            history.Items[0].ScoreAvailable.Should().BeTrue();
            history.Items[0].ReportKind.Should().Be("legacy_summary");
            history.Items[0].MatchMethod.Should().Be("hardcode");
        }

        [Fact]
        public async Task GetMatchHistoryAsync_HardcodeWithoutMethodProperty_CorrectlyIdentifiesHardcode()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);
            var userId = Guid.NewGuid();
            context.CvJobMatchScores.Add(new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Status = "Completed",
                MatchType = "Hardcode",
                MatchScore = 75.0m,
                MatchDetails = "{\"TitleScore\":100,\"SkillsScore\":70,\"FinalScore\":75.0}",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var history = await CreateDatabaseUseCase(context).GetMatchHistoryAsync(userId, 1, 10);

            history.Items.Should().ContainSingle();
            history.Items[0].ScorePercent.Should().Be(75.0m);
            history.Items[0].ScoreAvailable.Should().BeTrue();
            history.Items[0].MatchMethod.Should().Be("hardcode");
        }

        [Fact]
        public async Task GetMatchingResultAndHistory_UnscoredV5_KeepCompletedResultWithoutInventingZero()
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
                Status = "Completed",
                ProductScope = ITHunterview.Domain.Enums.CvJobMatchProductScope.CandidateOneToOne,
                MatchType = "AI",
                MatchScore = null,
                MatchDetails = "{\"contract\":\"jd-matching/v5\",\"scoreAvailable\":false,\"completionDisposition\":\"unscored_refundable\",\"jdFit\":{\"scorePercent\":null,\"resultCode\":\"INSUFFICIENT_DATA\",\"requirementGroups\":[],\"criticalGaps\":[]}}",
                UpdatedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var sut = CreateDatabaseUseCase(context);
            var result = await sut.GetMatchingResultAsync(matchId, userId);
            var history = await sut.GetMatchHistoryAsync(userId, 1, 10);

            result.Should().NotBeNull();
            result!.Status.Should().Be("Completed");
            result.ScorePercent.Should().BeNull();
            result.ScoreAvailable.Should().BeFalse();
            result.MatchDetails.Should().BeNull();
            result.CanRetry.Should().BeFalse();
            history.Items.Should().ContainSingle();
            history.Items[0].ScorePercent.Should().BeNull();
            history.Items[0].ScoreAvailable.Should().BeFalse();
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

        [Fact]
        public async Task GetJobMatchHistoryAsync_ShouldMaskCandidateDetails_WhenNotUnlocked()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);

            var jobId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            var candidateUserId = Guid.NewGuid();
            var recruiterId = Guid.NewGuid();

            context.Cvs.Add(new Cvs
            {
                Id = cvId,
                UserId = candidateUserId,
                FileName = "original_cv.pdf",
                FileUrl = "https://storage.local/original_cv.pdf",
                FileType = "pdf",
                ParsedData = "{}"
            });

            context.CvJobMatchScores.Add(new CvJobMatchScores
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                CvId = cvId,
                MatchScore = 85.5m
            });

            await context.SaveChangesAsync();

            var sut = new CvJobMatchingUseCase(
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

            // Act
            var result = await sut.GetJobMatchHistoryAsync(jobId, recruiterId, 1, 10);

            // Assert
            result.Should().NotBeNull();
            result.Items.Should().HaveCount(1);
            var item = result.Items[0];
            item.IsUnlocked.Should().BeFalse();
            item.CandidateId.Should().BeNull("CandidateId must be masked when locked");
            item.FileUrl.Should().BeNull("FileUrl must be masked when locked");
            item.CvFileName.Should().Be("Ứng viên #1", "FileName must be masked when locked");
        }

        [Fact]
        public async Task GetJobMatchHistoryAsync_MixedContracts_SortsNormalizedPercentBeforePaging()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);
            var jobId = Guid.NewGuid();
            var recruiterId = Guid.NewGuid();
            var v4Id = Guid.NewGuid();
            var hardcodeId = Guid.NewGuid();
            var v3Id = Guid.NewGuid();
            var vectorId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            context.CvJobMatchScores.AddRange(
                Score(v4Id, jobId, 91m, "AI", "{\"contract\":\"jd-matching/v4\",\"jdFit\":{\"scorePercent\":91}}", now),
                Score(hardcodeId, jobId, .85m, "Hardcode", "{\"Method\":\"HardcodeV3\",\"FinalScore\":0.85}", now.AddMinutes(-1)),
                Score(v3Id, jobId, 80m, "AI", "{\"contract\":\"jd-matching/v3\",\"jdFit\":{\"score\":80}}", now.AddMinutes(-2)),
                Score(vectorId, jobId, .75m, "Vector", "{\"TitleScore\":0.8,\"FinalScore\":0.75}", now.AddMinutes(-3)));
            await context.SaveChangesAsync();

            var page = await CreateDatabaseUseCase(context)
                .GetJobMatchHistoryAsync(jobId, recruiterId, 1, 2);

            page.TotalCount.Should().Be(4);
            page.Items.Select(item => item.SourceJobId).Should().Equal(v4Id, hardcodeId);
            page.Items.Select(item => item.ScorePercent).Should().Equal(91m, 85m);

            static CvJobMatchScores Score(
                Guid id,
                Guid sourceJobId,
                decimal score,
                string type,
                string details,
                DateTime updatedAt) => new()
                {
                    Id = id,
                    CvId = Guid.NewGuid(),
                    JobId = sourceJobId,
                    MatchScore = score,
                    MatchType = type,
                    MatchDetails = details,
                    UpdatedAt = updatedAt
                };
        }

        [Fact]
        public async Task UnlockCandidateCvAsync_ShouldReturnFail_WhenCvNotFound()
        {
            // Arrange
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            await using var context = new MatchingTestContext(options);

            var sut = new CvJobMatchingUseCase(
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

            var request = new UnlockCandidateRequestDto { CvId = Guid.NewGuid() };

            // Act
            var result = await sut.UnlockCandidateCvAsync(Guid.NewGuid(), request);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeFalse();
            result.Message.Should().Contain("Không tìm thấy hồ sơ CV");
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

        private sealed class BulkMatchingTestContext : ITHunterviewContext
        {
            public BulkMatchingTestContext(DbContextOptions<ITHunterviewContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                base.OnModelCreating(modelBuilder);
                var vectorConverter = new Microsoft.EntityFrameworkCore.Storage.ValueConversion.ValueConverter<Vector?, string?>(
                    vector => vector == null ? null : SerializeVector(vector),
                    value => value == null ? null : DeserializeVector(value));
                modelBuilder.Entity<Cvs>().Property(value => value.TitleEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<Cvs>().Property(value => value.SkillsEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<Cvs>().Property(value => value.ExperienceEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<Cvs>().Property(value => value.DomainEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<JobPostings>().Property(value => value.TitleEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<JobPostings>().Property(value => value.SkillsEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<JobPostings>().Property(value => value.ExperienceEmbedding).HasConversion(vectorConverter);
                modelBuilder.Entity<JobPostings>().Property(value => value.DomainEmbedding).HasConversion(vectorConverter);
                foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                             .Where(type => type.ClrType != typeof(Cvs)
                                            && type.ClrType != typeof(JobPostings)
                                            && type.ClrType != typeof(CvJobMatchScores)
                                            && type.ClrType != typeof(JobSkillRequirements)
                                            && type.ClrType != typeof(Skills)
                                            && type.ClrType != typeof(User)
                                            && type.ClrType != typeof(CandidateProfiles))
                             .Select(type => type.ClrType)
                             .Distinct()
                             .ToList())
                {
                    modelBuilder.Ignore(entityType);
                }
            }
        }

        private static string SerializeVector(Vector vector)
            => string.Join(",", vector.ToArray());

        private static Vector DeserializeVector(string value)
            => new(value.Split(',').Select(item => float.Parse(item)).ToArray());

        private static ITHunterviewContext CreateBulkMatchingContext()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;
            return new BulkMatchingTestContext(options);
        }

        private static (Cvs Cv, JobPostings Job, User User, CandidateProfiles Profile) CreateBulkMatchingEntities()
        {
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = "candidate@example.test",
                Status = UserStatus.ACTIVE
            };
            var profile = new CandidateProfiles
            {
                UserId = user.Id,
                IsVisibleToRecruiters = true,
                User = user
            };
            var embedding = new Vector(new float[] { 1, 0, 0 });
            var cv = new Cvs
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                IsPrimary = true,
                FileName = "cv.pdf",
                FileUrl = "https://example.test/cv.pdf",
                FileType = "application/pdf",
                RawText = "immutable CV source",
                ParsedData = "{\"matching_metrics\":{\"job_titles_normalized\":[\"Backend Developer\"],\"skills_normalized\":[\"C#\"],\"total_years_exp\":3,\"domains\":[\"fintech\"]}}",
                ParseStatus = "SUCCESS",
                TitleEmbedding = embedding,
                SkillsEmbedding = embedding,
                ExperienceEmbedding = embedding,
                DomainEmbedding = embedding,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var job = new JobPostings
            {
                Id = Guid.NewGuid(),
                JobCode = Guid.NewGuid().ToString("N"),
                RecruiterId = Guid.NewGuid(),
                CompanyId = Guid.NewGuid(),
                Title = "Backend Developer",
                Description = "Build APIs",
                Requirements = "C#, three years, fintech",
                Benefits = string.Empty,
                Currency = "VND",
                Location = "Remote",
                Status = JobStatus.PUBLISHED,
                ParseStatus = "SUCCESS",
                ParsedData = cv.ParsedData,
                TitleEmbedding = embedding,
                SkillsEmbedding = embedding,
                ExperienceEmbedding = embedding,
                DomainEmbedding = embedding,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            user.CandidateProfile = profile;
            user.Cvs.Add(cv);
            return (cv, job, user, profile);
        }

        private static CvJobMatchingUseCase CreateBulkMatchingUseCase(
            ITHunterviewContext context,
            string cvParsedData)
        {
            var validator = new Mock<ICvAnalysisResponseValidator>();
            validator
                .Setup(service => service.ValidateAndCanonicalize(cvParsedData))
                .Returns(CvAnalysisValidationResult.Complete(
                    cvParsedData,
                    new CvAnalysisCoverage(
                        1, 1, 0,
                        1, 1, 0,
                        1, 1, 0,
                        true, true, true, true)));
            return new CvJobMatchingUseCase(
                context,
                Mock.Of<IAiEmbeddingService>(),
                Mock.Of<ICvTextExtractorService>(),
                NullLogger<CvJobMatchingUseCase>.Instance,
                Mock.Of<IPromptManagementService>(),
                Mock.Of<IAiService>(),
                Mock.Of<ICandidateFeatureUsageUseCase>(),
                Mock.Of<IMatchingInputPreflightUseCase>(),
                Mock.Of<IMatchingSourceRepository>(),
                validator.Object);
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
