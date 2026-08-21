using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class HardcodeCvJobPairMatcherCharacterizationTests
{
    public static IEnumerable<object[]> CharacterizationCases()
    {
        foreach (var testCase in LoadFixture().Cases)
        {
            yield return new object[] { testCase };
        }
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public void LockedFixture_HasExactlyFourUniqueReviewedCases()
    {
        var names = LoadFixture().Cases.Select(testCase => testCase.Name).ToArray();

        names.Should().Equal(
            "structured_all_dimensions",
            "legacy_metrics_partial_cv",
            "partial_requirement_set_unscored",
            "no_safe_dimensions_unscored");
        names.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public void PairContract_AcceptsOnlyCvJobAndCancellation_AndReturnsExactResultContract()
    {
        var method = typeof(IHardcodeCvJobPairMatcher).GetMethods().Should().ContainSingle().Subject;
        method.Name.Should().Be("MatchAsync");
        method.ReturnType.Should().Be(typeof(Task<HardcodePairMatchResult>));
        method.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(Cvs),
            typeof(JobPostings),
            typeof(CancellationToken));

        var resultType = typeof(HardcodePairMatchResult);
        resultType.IsSealed.Should().BeTrue();
        resultType.GetMethod("<Clone>$", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .Should().NotBeNull("the public contract is a record, not a mutable result bag");
        resultType.GetInterfaces().Should().Contain(typeof(IEquatable<HardcodePairMatchResult>));
        var resultConstructor = resultType.GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Should().ContainSingle().Subject;
        resultConstructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(decimal?),
            typeof(string),
            typeof(CvAnalysisQuality?),
            typeof(string),
            typeof(string));
        resultConstructor.GetParameters().Select(parameter => parameter.Name).Should().Equal(
            "MatchScore",
            "MatchDetails",
            "CvAnalysisQuality",
            "CvAnalysisCoverageJson",
            "CvAnalysisDiagnosticsJson");
        var resultProperties = resultType.GetProperties(BindingFlags.Instance | BindingFlags.Public);
        resultProperties.Select(property => property.Name).Should().Equal(
            "MatchScore",
            "MatchDetails",
            "CvAnalysisQuality",
            "CvAnalysisCoverageJson",
            "CvAnalysisDiagnosticsJson");
        resultProperties.Select(property => property.PropertyType).Should().Equal(
            typeof(decimal?),
            typeof(string),
            typeof(CvAnalysisQuality?),
            typeof(string),
            typeof(string));
        resultProperties.Should().OnlyContain(property =>
            property.SetMethod != null &&
            property.SetMethod.ReturnParameter.GetRequiredCustomModifiers().Contains(typeof(IsExternalInit)),
            "the positional result record exposes init-only properties and no mutable setter");
        resultType.GetFields(BindingFlags.Instance | BindingFlags.Public).Should().BeEmpty();

        var matcherConstructor = typeof(HardcodeCvJobPairMatcher)
            .GetConstructors(BindingFlags.Instance | BindingFlags.Public)
            .Should().ContainSingle().Subject;
        matcherConstructor.GetParameters().Select(parameter => parameter.ParameterType).Should().Equal(
            typeof(ITHunterviewContext),
            typeof(ICvTextExtractorService),
            typeof(ILogger<HardcodeCvJobPairMatcher>),
            typeof(HardcodeJdRequirementScoringService),
            typeof(ICvAnalysisResponseValidator));
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_PreCanceledToken_PerformsNoPreparationPersistenceOrCvMutation()
    {
        var fixture = LoadFixture();
        var rawCv = fixture.CvAnalyses["complete"];
        await using var context = CreateContext();
        var cv = CreateCv(Guid.Parse("10000000-0000-0000-0000-000000000020"), default);
        var job = CreateJob(120, JsonDocument.Parse("""
            {"matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[]}}
            """).RootElement.Clone());
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();
        var saveCallsBeforeMatch = context.SaveChangesAsyncCallCount;
        var fallbackAccessesBeforeMatch = context.JobSkillFallbackAccessCount;
        var parsedDataBeforeMatch = cv.ParsedData;
        var parseStatusBeforeMatch = cv.ParseStatus;
        var parseErrorBeforeMatch = cv.ParseError;
        var qualityBeforeMatch = cv.AnalysisQuality;
        var coverageBeforeMatch = cv.AnalysisCoverageJson;
        var diagnosticsBeforeMatch = cv.AnalysisDiagnosticsJson;

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!))
            .ReturnsAsync(rawCv.GetRawText());
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(
                cv.FileUrl,
                cv.RawText!,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(rawCv.GetRawText());
        var matcher = CreateMatcher(context, extractor.Object, new CvAnalysisResponseValidator());
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        var exception = await Record.ExceptionAsync(
            () => matcher.MatchAsync(cv, job, cancellation.Token));

        context.SaveChangesAsyncCallCount.Should().Be(saveCallsBeforeMatch);
        context.JobSkillFallbackAccessCount.Should().Be(fallbackAccessesBeforeMatch);
        cv.ParsedData.Should().Be(parsedDataBeforeMatch);
        cv.ParseStatus.Should().Be(parseStatusBeforeMatch);
        cv.ParseError.Should().Be(parseErrorBeforeMatch);
        cv.AnalysisQuality.Should().Be(qualityBeforeMatch);
        cv.AnalysisCoverageJson.Should().Be(coverageBeforeMatch);
        cv.AnalysisDiagnosticsJson.Should().Be(diagnosticsBeforeMatch);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        var cancellationException = Assert.IsAssignableFrom<OperationCanceledException>(exception);
        cancellationException.CancellationToken.Should().Be(cancellation.Token);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_InFlightSuppliedCancellation_PropagatesWithoutInvalidPersistenceAndCanRetrySameScope()
    {
        var fixture = LoadFixture();
        var rawCv = fixture.CvAnalyses["complete"];
        var expectedCv = fixture.ExpectedCvAnalyses["complete"];
        var testCase = fixture.Cases.Single(value => value.Name == "structured_all_dimensions");
        await using var context = CreateContext();
        var cv = CreateCv(Guid.Parse("10000000-0000-0000-0000-000000000021"), default);
        var job = CreateJob(130, testCase.JobAnalysis);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();
        var saveCallsBeforeMatch = context.SaveChangesAsyncCallCount;
        var extractionStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var legacyShutdown = new CancellationTokenSource();
        var suppliedTokens = new List<CancellationToken>();
        var tokenExtractionAttempts = 0;

        async Task<string> ExtractWithoutTokenAsync()
        {
            extractionStarted.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, legacyShutdown.Token);
            return rawCv.GetRawText();
        }

        async Task<string> ExtractWithTokenAsync(string _, string __, CancellationToken token)
        {
            suppliedTokens.Add(token);
            extractionStarted.TrySetResult(true);
            if (Interlocked.Increment(ref tokenExtractionAttempts) == 1)
            {
                await Task.Delay(Timeout.Infinite, token);
            }
            return rawCv.GetRawText();
        }

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!))
            .Returns(ExtractWithoutTokenAsync);
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(
                cv.FileUrl,
                cv.RawText!,
                It.IsAny<CancellationToken>()))
            .Returns(ExtractWithTokenAsync);
        var matcher = CreateMatcher(context, extractor.Object, new CvAnalysisResponseValidator());
        using var cancellation = new CancellationTokenSource();

        var matchTask = matcher.MatchAsync(cv, job, cancellation.Token);
        await extractionStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
        cancellation.Cancel();
        var completedBeforeCleanup = await Task.WhenAny(matchTask, Task.Delay(TimeSpan.FromSeconds(1))) == matchTask;
        if (!completedBeforeCleanup)
        {
            legacyShutdown.Cancel();
        }
        var cancellationException = await Record.ExceptionAsync(async () => await matchTask);
        var saveCallsAfterCancellation = context.SaveChangesAsyncCallCount;
        var persistedAfterCancellation = await context.Cvs.AsNoTracking().SingleAsync(value => value.Id == cv.Id);

        HardcodePairMatchResult? retryResult = null;
        using var retryCancellation = new CancellationTokenSource();
        var retryException = await Record.ExceptionAsync(async () =>
            retryResult = await matcher.MatchAsync(cv, job, retryCancellation.Token));

        completedBeforeCleanup.Should().BeTrue("the supplied token must cancel the in-flight extractor call");
        var propagated = Assert.IsAssignableFrom<OperationCanceledException>(cancellationException);
        propagated.CancellationToken.Should().Be(cancellation.Token);
        saveCallsAfterCancellation.Should().Be(saveCallsBeforeMatch);
        persistedAfterCancellation.ParseStatus.Should().Be("PENDING");
        persistedAfterCancellation.ParseError.Should().BeNull();
        persistedAfterCancellation.AnalysisQuality.Should().BeNull();
        persistedAfterCancellation.AnalysisCoverageJson.Should().BeNull();
        persistedAfterCancellation.AnalysisDiagnosticsJson.Should().BeNull();
        retryException.Should().BeNull("a canceled preparation must be evicted from the matcher-instance cache");
        retryResult.Should().NotBeNull();
        retryResult!.MatchScore.Should().Be(testCase.ExpectedMatchScore);
        AssertExactJson(retryResult.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "same-scope retry result");
        AssertExactCvMetadata(retryResult, cv, expectedCv);
        suppliedTokens.Should().Equal(cancellation.Token, retryCancellation.Token);
        tokenExtractionAttempts.Should().Be(2);
    }

    [Theory]
    [Trait("Requirement", "R-04")]
    [MemberData(nameof(CharacterizationCases))]
    public async Task MatchAsync_FixedPair_IsEquivalentToPreRefactorStoredResult(
        HardcodeGoldenCase testCase)
    {
        var fixture = LoadFixture();
        var expectedCv = fixture.ExpectedCvAnalyses[testCase.CvAnalysis];
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000011");

        await using var legacyContext = CreateContext();
        var legacyCv = CreateCv(userId, fixture.CvAnalyses[testCase.CvAnalysis]);
        var legacyJob = CreateJob(testCase.Ordinal, testCase.JobAnalysis);
        legacyContext.Cvs.Add(legacyCv);
        legacyContext.JobPostings.Add(legacyJob);
        await legacyContext.SaveChangesAsync();

        await CreateLegacyUseCase(legacyContext).MatchCvWithAllJobsHardcodeAsync(legacyCv.Id, userId);
        var stored = await legacyContext.CvJobMatchScores.SingleAsync();

        await using var adapterContext = CreateContext();
        var adapterCv = CreateCv(userId, fixture.CvAnalyses[testCase.CvAnalysis]);
        var adapterJob = CreateJob(testCase.Ordinal, testCase.JobAnalysis);
        adapterContext.Cvs.Add(adapterCv);
        adapterContext.JobPostings.Add(adapterJob);
        await adapterContext.SaveChangesAsync();

        var actual = await CreateMatcher(adapterContext).MatchAsync(adapterCv, adapterJob);

        actual.MatchScore.Should().Be(testCase.ExpectedMatchScore);
        actual.MatchScore.Should().Be(stored.MatchScore);
        AssertExactJson(actual.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "adapter match details");
        AssertExactJson(actual.MatchDetails, stored.MatchDetails, "pre-refactor stored match details");
        AssertExactCvMetadata(actual, adapterCv, expectedCv);
        actual.CvAnalysisQuality.Should().Be(stored.CvAnalysisQuality);
        AssertExactJson(actual.CvAnalysisCoverageJson!, stored.CvAnalysisCoverageJson!, "pre-refactor CV coverage");
        AssertExactJson(actual.CvAnalysisDiagnosticsJson!, stored.CvAnalysisDiagnosticsJson!, "pre-refactor CV diagnostics");
        AssertCanonicalCv(adapterCv, expectedCv);
        stored.Status.Should().Be(testCase.ExpectedStatus);
        stored.MatchType.Should().Be(testCase.ExpectedMatchType);
        stored.ErrorCode.Should().BeNull();
        stored.ErrorMessage.Should().BeNull();
        (await adapterContext.CvJobMatchScores.CountAsync()).Should().Be(0,
            "the pair adapter returns a value and never creates a shared result row");
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_DefaultAndExplicitNone_RemainExactLegacyFacadeEquivalent()
    {
        var fixture = LoadFixture();
        var testCase = fixture.Cases.Single(value => value.Name == "structured_all_dimensions");
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000022");

        await using var legacyContext = CreateContext();
        var legacyCv = CreateCv(userId, fixture.CvAnalyses[testCase.CvAnalysis]);
        var legacyJob = CreateJob(140, testCase.JobAnalysis);
        legacyContext.Cvs.Add(legacyCv);
        legacyContext.JobPostings.Add(legacyJob);
        await legacyContext.SaveChangesAsync();
        await CreateLegacyUseCase(legacyContext).MatchCvWithAllJobsHardcodeAsync(legacyCv.Id, userId);
        var stored = await legacyContext.CvJobMatchScores.SingleAsync();

        await using var defaultContext = CreateContext();
        var defaultCv = CreateCv(userId, fixture.CvAnalyses[testCase.CvAnalysis]);
        var defaultJob = CreateJob(140, testCase.JobAnalysis);
        defaultContext.Cvs.Add(defaultCv);
        defaultContext.JobPostings.Add(defaultJob);
        await defaultContext.SaveChangesAsync();
        var defaultResult = await CreateMatcher(defaultContext).MatchAsync(defaultCv, defaultJob);

        await using var noneContext = CreateContext();
        var noneCv = CreateCv(userId, fixture.CvAnalyses[testCase.CvAnalysis]);
        var noneJob = CreateJob(140, testCase.JobAnalysis);
        noneContext.Cvs.Add(noneCv);
        noneContext.JobPostings.Add(noneJob);
        await noneContext.SaveChangesAsync();
        var noneResult = await CreateMatcher(noneContext).MatchAsync(noneCv, noneJob, CancellationToken.None);

        foreach (var result in new[] { defaultResult, noneResult })
        {
            result.MatchScore.Should().Be(testCase.ExpectedMatchScore);
            result.MatchScore.Should().Be(stored.MatchScore);
            AssertExactJson(result.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "default/None fixture result");
            AssertExactJson(result.MatchDetails, stored.MatchDetails, "default/None legacy facade result");
            result.CvAnalysisQuality.Should().Be(stored.CvAnalysisQuality);
            result.CvAnalysisCoverageJson.Should().Be(stored.CvAnalysisCoverageJson);
            result.CvAnalysisDiagnosticsJson.Should().Be(stored.CvAnalysisDiagnosticsJson);
        }
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task ExistingSharedResultRows_AreNeverReadOrMutated()
    {
        var fixture = LoadFixture();
        var testCase = fixture.Cases.Single(value => value.Name == "structured_all_dimensions");
        await using var context = CreateContext();
        var userId = Guid.Parse("10000000-0000-0000-0000-000000000016");
        var cv = CreateCv(userId, fixture.CvAnalyses["complete"]);
        var job = CreateJob(80, testCase.JobAnalysis);
        var sentinelUpdatedAt = FixedUtc.AddDays(-1);
        var sentinel = new CvJobMatchScores
        {
            Id = Guid.Parse("60000000-0000-0000-0000-000000000016"),
            UserId = userId,
            CvId = cv.Id,
            JobId = job.Id,
            RawJdText = "Synthetic legacy sentinel",
            MatchScore = 13.37m,
            MatchDetails = "{\"sentinel\":\"legacy-shared-result\"}",
            MatchType = "Vector",
            Status = "Completed",
            ErrorCode = "SENTINEL_ERROR",
            ErrorMessage = "Synthetic sentinel error",
            CreatedAt = sentinelUpdatedAt,
            UpdatedAt = sentinelUpdatedAt
        };
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        context.CvJobMatchScores.Add(sentinel);
        await context.SaveChangesAsync();
        context.Entry(sentinel).State.Should().Be(EntityState.Unchanged);
        context.RejectSharedResultAccess = true;

        HardcodePairMatchResult result;
        try
        {
            result = await CreateMatcher(context).MatchAsync(cv, job);
        }
        finally
        {
            context.RejectSharedResultAccess = false;
        }

        result.MatchScore.Should().Be(testCase.ExpectedMatchScore,
            "an existing shared row is not a pair-result cache");
        AssertExactJson(result.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "result independent of shared sentinel");
        var rows = await context.CvJobMatchScores.ToListAsync();
        rows.Should().ContainSingle().Which.Should().BeSameAs(sentinel);
        sentinel.MatchScore.Should().Be(13.37m);
        sentinel.MatchDetails.Should().Be("{\"sentinel\":\"legacy-shared-result\"}");
        sentinel.MatchType.Should().Be("Vector");
        sentinel.Status.Should().Be("Completed");
        sentinel.ErrorCode.Should().Be("SENTINEL_ERROR");
        sentinel.ErrorMessage.Should().Be("Synthetic sentinel error");
        sentinel.UpdatedAt.Should().Be(sentinelUpdatedAt);
        context.Entry(sentinel).State.Should().Be(EntityState.Unchanged);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    public async Task PreparedCv_ManyPairs_ExtractsAndCanonicalizesAtMostOnce()
    {
        var fixture = LoadFixture();
        var rawCv = fixture.CvAnalyses["complete"];
        var expectedCv = fixture.ExpectedCvAnalyses["complete"];
        var testCase = fixture.Cases.Single(value => value.Name == "structured_all_dimensions");
        await using var context = CreateContext();
        var cv = CreateCv(Guid.Parse("10000000-0000-0000-0000-000000000012"), default);
        cv.ParsedData = string.Empty;
        cv.ParseStatus = "PENDING";
        var jobs = Enumerable.Range(0, 3)
            .Select(index => CreateJob(index + 40, testCase.JobAnalysis))
            .ToArray();
        context.Cvs.Add(cv);
        context.JobPostings.AddRange(jobs);
        await context.SaveChangesAsync();

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!))
            .ReturnsAsync(rawCv.GetRawText());
        var realValidator = new CvAnalysisResponseValidator();
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(service => service.ValidateAndCanonicalize(rawCv.GetRawText()))
            .Returns(realValidator.ValidateAndCanonicalize(rawCv.GetRawText()));
        var matcher = CreateMatcher(context, extractor.Object, validator.Object);

        var results = new List<HardcodePairMatchResult>();
        foreach (var job in jobs)
        {
            results.Add(await matcher.MatchAsync(cv, job));
        }

        results.Should().HaveCount(3);
        foreach (var result in results)
        {
            result.MatchScore.Should().Be(testCase.ExpectedMatchScore);
            AssertExactJson(result.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "reused prepared-CV result");
            AssertExactCvMetadata(result, cv, expectedCv);
        }
        cv.ParseStatus.Should().Be("SUCCESS");
        AssertCanonicalCv(cv, expectedCv);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!), Times.Once);
        validator.Verify(service => service.ValidateAndCanonicalize(rawCv.GetRawText()), Times.Once);
        (await context.CvJobMatchScores.CountAsync()).Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_DefaultAndExplicitNone_SameInstancePreparesCvOnce()
    {
        var fixture = LoadFixture();
        var rawCv = fixture.CvAnalyses["complete"];
        var testCase = fixture.Cases.Single(value => value.Name == "structured_all_dimensions");
        await using var context = CreateContext();
        var cv = CreateCv(Guid.Parse("10000000-0000-0000-0000-000000000023"), default);
        var defaultJob = CreateJob(150, testCase.JobAnalysis);
        var noneJob = CreateJob(151, testCase.JobAnalysis);
        context.Cvs.Add(cv);
        context.JobPostings.AddRange(defaultJob, noneJob);
        await context.SaveChangesAsync();

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        extractor.Setup(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!))
            .ReturnsAsync(rawCv.GetRawText());
        var realValidator = new CvAnalysisResponseValidator();
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(service => service.ValidateAndCanonicalize(rawCv.GetRawText()))
            .Returns(realValidator.ValidateAndCanonicalize(rawCv.GetRawText()));
        var matcher = CreateMatcher(context, extractor.Object, validator.Object);

        var defaultResult = await matcher.MatchAsync(cv, defaultJob);
        var noneResult = await matcher.MatchAsync(cv, noneJob, CancellationToken.None);

        foreach (var result in new[] { defaultResult, noneResult })
        {
            result.MatchScore.Should().Be(testCase.ExpectedMatchScore);
            AssertExactJson(result.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "default/None cached result");
        }
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(cv.FileUrl, cv.RawText!), Times.Once);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
        validator.Verify(service => service.ValidateAndCanonicalize(rawCv.GetRawText()), Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-03")]
    public async Task AlreadyUsableCv_PairMatch_DoesNotReparse()
    {
        var fixture = LoadFixture();
        var testCase = fixture.Cases.Single(value => value.Name == "legacy_metrics_partial_cv");
        var expectedCv = fixture.ExpectedCvAnalyses[testCase.CvAnalysis];
        await using var context = CreateContext();
        var cv = CreateCv(
            Guid.Parse("10000000-0000-0000-0000-000000000013"),
            fixture.CvAnalyses[testCase.CvAnalysis]);
        var job = CreateJob(50, testCase.JobAnalysis);
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();
        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);

        var result = await CreateMatcher(context, extractor.Object, new CvAnalysisResponseValidator())
            .MatchAsync(cv, job);

        result.MatchScore.Should().Be(testCase.ExpectedMatchScore);
        AssertExactJson(result.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "already-usable CV result");
        AssertExactCvMetadata(result, cv, expectedCv);
        AssertCanonicalCv(cv, expectedCv);
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        (await context.CvJobMatchScores.CountAsync()).Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-02")]
    [Trait("Requirement", "R-03")]
    public async Task FreshMatcherScope_SameCv_PreparesAgainForNewRun()
    {
        var fixture = LoadFixture();
        var testCase = fixture.Cases.Single(value => value.Name == "structured_all_dimensions");
        await using var context = CreateContext();
        var cv = CreateCv(
            Guid.Parse("10000000-0000-0000-0000-000000000015"),
            fixture.CvAnalyses["complete"]);
        var jobs = Enumerable.Range(0, 3)
            .Select(index => CreateJob(index + 70, testCase.JobAnalysis))
            .ToArray();
        context.Cvs.Add(cv);
        context.JobPostings.AddRange(jobs);
        await context.SaveChangesAsync();

        var extractor = new Mock<ICvTextExtractorService>(MockBehavior.Strict);
        var realValidator = new CvAnalysisResponseValidator();
        var validator = new Mock<ICvAnalysisResponseValidator>(MockBehavior.Strict);
        validator.Setup(service => service.ValidateAndCanonicalize(It.IsAny<string>()))
            .Returns((string json) => realValidator.ValidateAndCanonicalize(json));

        var firstRunMatcher = CreateMatcher(context, extractor.Object, validator.Object);
        var firstResult = await firstRunMatcher.MatchAsync(cv, jobs[0]);
        var sameRunResult = await firstRunMatcher.MatchAsync(cv, jobs[1]);
        var nextRunMatcher = CreateMatcher(context, extractor.Object, validator.Object);
        var nextRunResult = await nextRunMatcher.MatchAsync(cv, jobs[2]);

        foreach (var result in new[] { firstResult, sameRunResult, nextRunResult })
        {
            result.MatchScore.Should().Be(testCase.ExpectedMatchScore);
            AssertExactJson(result.MatchDetails, testCase.ExpectedMatchDetails.GetRawText(), "run-scoped prepared-CV result");
        }
        validator.Verify(service => service.ValidateAndCanonicalize(It.IsAny<string>()), Times.Exactly(2));
        extractor.Verify(service => service.ExtractParsedDataFromUrlAsync(
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
        (await context.CvJobMatchScores.CountAsync()).Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_LegacyJobWithoutSkillArray_UsesRecruiterApprovedSkillFallback()
    {
        var fixture = LoadFixture();
        await using var context = CreateContext();
        var cv = CreateCv(
            Guid.Parse("10000000-0000-0000-0000-000000000014"),
            fixture.CvAnalyses["complete"]);
        var job = CreateJob(60, JsonDocument.Parse("""
            {"matching_metrics":{"job_titles_normalized":[],"skills_normalized":[],"total_years_exp":0,"domains":[]}}
            """).RootElement.Clone());
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        context.Skills.AddRange(
            new Skills { Id = 901, Name = "C#", NormalizedName = "c#" },
            new Skills { Id = 902, Name = "Go", NormalizedName = "go" });
        context.JobSkillRequirements.AddRange(
            new JobSkillRequirements { JobId = job.Id, SkillId = 901, IsMandatory = true },
            new JobSkillRequirements { JobId = job.Id, SkillId = 902, IsMandatory = true });
        await context.SaveChangesAsync();

        var result = await CreateMatcher(context).MatchAsync(cv, job);

        result.MatchScore.Should().Be(50m);
        AssertExactJson(result.MatchDetails, """
            {
              "Method":"Hardcode",
              "JdSchemaVersion":"legacy",
              "TitleScore":50.0,
              "SkillsScore":50.0,
              "ExperienceScore":50.0,
              "DomainScore":50.0,
              "FinalScore":50.0,
              "ScoreBasis":"available_cv_metrics",
              "CvAnalysisQuality":"COMPLETE",
              "AvailableDimensions":["skills"],
              "Weights":{"TitleWeight":0.15,"SkillsWeight":0.45,"ExperienceWeight":0.30,"DomainWeight":0.10},
              "GroupOutcomes":null
            }
            """, "legacy Job skill fallback result");
        (await context.CvJobMatchScores.CountAsync()).Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_DeclaredStructuredJdWithoutUsableGroups_ReturnsStructuredUnavailableReason()
    {
        var fixture = LoadFixture();
        await using var context = CreateContext();
        var cv = CreateCv(
            Guid.Parse("10000000-0000-0000-0000-000000000017"),
            fixture.CvAnalyses["complete"]);
        var job = CreateJob(90, JsonDocument.Parse("""
            {"schema_version":"jd-analysis-effective/v1","analysis_quality":"COMPLETE","matching_metrics":{"requirement_groups":"invalid"}}
            """).RootElement.Clone());
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        var result = await CreateMatcher(context).MatchAsync(cv, job);

        result.MatchScore.Should().BeNull();
        AssertExactJson(result.MatchDetails, """
            {
              "Method":"Hardcode",
              "ScoreBasis":"no_safe_dimensions",
              "ResultCode":"SCORE_UNAVAILABLE",
              "InternalReasonCode":"STRUCTURED_JD_UNAVAILABLE",
              "CvAnalysisQuality":"COMPLETE",
              "JdAnalysisQuality":"INVALID",
              "JdSchemaVersion":"jd-analysis-effective/v1",
              "GroupOutcomes":null
            }
            """, "declared structured JD without usable groups");
        (await context.CvJobMatchScores.CountAsync()).Should().Be(0);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_FractionalDimensions_RoundsJsonComponentsAndFinalScoreToTwoDecimals()
    {
        var fixture = LoadFixture();
        await using var context = CreateContext();
        var cv = CreateCv(
            Guid.Parse("10000000-0000-0000-0000-000000000018"),
            fixture.CvAnalyses["complete"]);
        var job = CreateJob(100, JsonDocument.Parse("""
            {"matching_metrics":{"job_titles_normalized":["Backend Engineer"],"skills_normalized":["C#","Go","Rust"],"total_years_exp":7,"domains":["healthcare"]}}
            """).RootElement.Clone());
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        var result = await CreateMatcher(context).MatchAsync(cv, job);

        using var details = JsonDocument.Parse(result.MatchDetails);
        details.RootElement.GetProperty("SkillsScore").GetDecimal().Should().Be(33.33m);
        details.RootElement.GetProperty("ExperienceScore").GetDecimal().Should().Be(57.14m);
        details.RootElement.GetProperty("FinalScore").GetDecimal().Should().Be(50.14m);
    }

    [Fact]
    [Trait("Requirement", "R-04")]
    public async Task MatchAsync_TitleSkillAndDomainComparison_RemainsCaseInsensitive()
    {
        var fixture = LoadFixture();
        var cvAnalysis = JsonNode.Parse(fixture.CvAnalyses["complete"].GetRawText())!.AsObject();
        var metrics = cvAnalysis["matching_metrics"]!.AsObject();
        metrics["job_titles_normalized"] = new JsonArray("BACKEND ENGINEER");
        metrics["skills_normalized"] = new JsonArray("c#", "postgresql");
        metrics["domains"] = new JsonArray("FINTECH");
        using var cvAnalysisDocument = JsonDocument.Parse(cvAnalysis.ToJsonString());
        await using var context = CreateContext();
        var cv = CreateCv(
            Guid.Parse("10000000-0000-0000-0000-000000000019"),
            cvAnalysisDocument.RootElement.Clone());
        var job = CreateJob(110, JsonDocument.Parse("""
            {"matching_metrics":{"job_titles_normalized":["Backend Engineer"],"skills_normalized":["C#","PostgreSQL"],"total_years_exp":4,"domains":["fintech"]}}
            """).RootElement.Clone());
        context.Cvs.Add(cv);
        context.JobPostings.Add(job);
        await context.SaveChangesAsync();

        var result = await CreateMatcher(context).MatchAsync(cv, job);

        result.MatchScore.Should().Be(100m);
        using var details = JsonDocument.Parse(result.MatchDetails);
        details.RootElement.GetProperty("TitleScore").GetDecimal().Should().Be(100m);
        details.RootElement.GetProperty("SkillsScore").GetDecimal().Should().Be(100m);
        details.RootElement.GetProperty("DomainScore").GetDecimal().Should().Be(100m);
    }

    private static HardcodeCvJobPairMatcher CreateMatcher(ITHunterviewContext context) =>
        CreateMatcher(
            context,
            new Mock<ICvTextExtractorService>(MockBehavior.Strict).Object,
            new CvAnalysisResponseValidator());

    private static HardcodeCvJobPairMatcher CreateMatcher(
        ITHunterviewContext context,
        ICvTextExtractorService extractor,
        ICvAnalysisResponseValidator validator) => new(
            context,
            extractor,
            NullLogger<HardcodeCvJobPairMatcher>.Instance,
            new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator()),
            validator);

    private static HardcodeCvJobMatchingUseCase CreateLegacyUseCase(ITHunterviewContext context) => new(
        context,
        new Mock<ICvTextExtractorService>(MockBehavior.Strict).Object,
        NullLogger<HardcodeCvJobMatchingUseCase>.Instance,
        new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator()),
        new CvAnalysisResponseValidator());

    private static void AssertExactCvMetadata(
        HardcodePairMatchResult result,
        Cvs cv,
        ExpectedCvAnalysis expected)
    {
        result.CvAnalysisQuality?.ToString().Should().Be(expected.Quality);
        result.CvAnalysisQuality.Should().Be(cv.AnalysisQuality);
        result.CvAnalysisCoverageJson.Should().Be(cv.AnalysisCoverageJson);
        result.CvAnalysisDiagnosticsJson.Should().Be(cv.AnalysisDiagnosticsJson);
        AssertExactJson(result.CvAnalysisCoverageJson!, expected.Coverage.GetRawText(), "adapter CV coverage");
        AssertExactJson(result.CvAnalysisDiagnosticsJson!, expected.Diagnostics.GetRawText(), "adapter CV diagnostics");
    }

    private static void AssertCanonicalCv(Cvs cv, ExpectedCvAnalysis expected)
    {
        AssertExactJson(cv.ParsedData!, expected.Canonical.GetRawText(), "canonical saved CV analysis");
        cv.AnalysisQuality?.ToString().Should().Be(expected.Quality);
        AssertExactJson(cv.AnalysisCoverageJson!, expected.Coverage.GetRawText(), "saved CV coverage");
        AssertExactJson(cv.AnalysisDiagnosticsJson!, expected.Diagnostics.GetRawText(), "saved CV diagnostics");
    }

    private static void AssertExactJson(string actualJson, string expectedJson, string because)
    {
        using var actual = JsonDocument.Parse(actualJson);
        using var expected = JsonDocument.Parse(expectedJson);
        JsonElement.DeepEquals(actual.RootElement, expected.RootElement).Should().BeTrue(
            $"{because} must be semantically identical; actual={actualJson}; expected={expectedJson}");
        AssertPropertyOrder(actual.RootElement, expected.RootElement, "$", because);
    }

    private static void AssertPropertyOrder(
        JsonElement actual,
        JsonElement expected,
        string path,
        string because)
    {
        if (expected.ValueKind == JsonValueKind.Object)
        {
            var actualProperties = actual.EnumerateObject().ToArray();
            var expectedProperties = expected.EnumerateObject().ToArray();
            actualProperties.Select(property => property.Name).Should().Equal(
                expectedProperties.Select(property => property.Name),
                $"{because} must preserve JSON property names and order at {path}");
            for (var index = 0; index < expectedProperties.Length; index++)
            {
                AssertPropertyOrder(
                    actualProperties[index].Value,
                    expectedProperties[index].Value,
                    $"{path}.{expectedProperties[index].Name}",
                    because);
            }
            return;
        }

        if (expected.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var actualItems = actual.EnumerateArray().ToArray();
        var expectedItems = expected.EnumerateArray().ToArray();
        actualItems.Should().HaveCount(expectedItems.Length);
        for (var index = 0; index < expectedItems.Length; index++)
        {
            AssertPropertyOrder(actualItems[index], expectedItems[index], $"{path}[{index}]", because);
        }
    }

    private static Cvs CreateCv(Guid userId, JsonElement analysis) => new()
    {
        Id = Guid.Parse("20000000-0000-0000-0000-000000000011"),
        UserId = userId,
        FileUrl = "https://synthetic.example.test/candidate.pdf",
        FileName = "synthetic-candidate.pdf",
        FileType = "application/pdf",
        ParsedData = analysis.ValueKind == JsonValueKind.Undefined ? string.Empty : analysis.GetRawText(),
        ParseStatus = analysis.ValueKind == JsonValueKind.Undefined ? "PENDING" : "SUCCESS",
        RawText = "Synthetic saved CV source.",
        CreatedAt = FixedUtc,
        UpdatedAt = FixedUtc,
        IsPrimary = true
    };

    private static JobPostings CreateJob(int ordinal, JsonElement analysis) => new()
    {
        Id = Guid.Parse($"30000000-0000-0000-0000-{ordinal + 1:000000000000}"),
        JobCode = $"SYNTHETIC-PAIR-{ordinal + 1:000}",
        RecruiterId = Guid.Parse("40000000-0000-0000-0000-000000000011"),
        CompanyId = Guid.Parse("50000000-0000-0000-0000-000000000011"),
        Title = "Backend Engineer",
        Description = "Build synthetic APIs.",
        Requirements = "Synthetic requirements only.",
        Benefits = string.Empty,
        Currency = "VND",
        Location = "Remote",
        Status = JobStatus.PUBLISHED,
        ParseStatus = "SUCCESS",
        ParsedData = analysis.GetRawText(),
        CreatedAt = FixedUtc,
        UpdatedAt = FixedUtc
    };

    private static CharacterizationFixture LoadFixture()
    {
        var path = Path.Combine(
            AppContext.BaseDirectory,
            "Matching",
            "Fixtures",
            "hardcode-characterization-cases.json");
        return JsonSerializer.Deserialize<CharacterizationFixture>(File.ReadAllText(path), JsonOptions)
               ?? throw new InvalidOperationException("Characterization fixture could not be loaded.");
    }

    private static PairMatcherTestContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PairMatcherTestContext(options);
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly DateTime FixedUtc = new(2026, 8, 16, 0, 0, 0, DateTimeKind.Utc);

    private sealed record CharacterizationFixture(
        Dictionary<string, JsonElement> CvAnalyses,
        Dictionary<string, ExpectedCvAnalysis> ExpectedCvAnalyses,
        IReadOnlyList<HardcodeGoldenCase> Cases);

    private sealed record ExpectedCvAnalysis(
        string Quality,
        JsonElement Coverage,
        JsonElement Diagnostics,
        JsonElement Canonical);

    public sealed record HardcodeGoldenCase(
        string Name,
        string CvAnalysis,
        decimal? ExpectedMatchScore,
        string ExpectedStatus,
        string ExpectedMatchType,
        JsonElement ExpectedMatchDetails,
        JsonElement JobAnalysis)
    {
        public int Ordinal => Name switch
        {
            "structured_all_dimensions" => 0,
            "legacy_metrics_partial_cv" => 1,
            "partial_requirement_set_unscored" => 2,
            "no_safe_dimensions_unscored" => 3,
            _ => throw new InvalidOperationException($"Unknown characterization case '{Name}'.")
        };

        public override string ToString() => Name;
    }

    private sealed class PairMatcherTestContext : ITHunterviewContext
    {
        public PairMatcherTestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        public bool RejectSharedResultAccess { get; set; }

        public int SaveChangesAsyncCallCount { get; private set; }

        public int JobSkillFallbackAccessCount { get; private set; }

        public override DbSet<Skills> Skills
        {
            get
            {
                JobSkillFallbackAccessCount++;
                return base.Skills;
            }
            set => base.Skills = value;
        }

        public override DbSet<JobSkillRequirements> JobSkillRequirements
        {
            get
            {
                JobSkillFallbackAccessCount++;
                return base.JobSkillRequirements;
            }
            set => base.JobSkillRequirements = value;
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesAsyncCallCount++;
            return base.SaveChangesAsync(cancellationToken);
        }

        public override DbSet<CvJobMatchScores> CvJobMatchScores
        {
            get => RejectSharedResultAccess
                ? throw new InvalidOperationException("PAIR_MATCHER_MUST_NOT_READ_SHARED_RESULTS")
                : base.CvJobMatchScores;
            set => base.CvJobMatchScores = value;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
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

            modelBuilder.Entity<Cvs>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<Cvs>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.TitleEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.SkillsEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.ExperienceEmbedding);
            modelBuilder.Entity<JobPostings>().Ignore(value => value.DomainEmbedding);
            modelBuilder.Entity<Skills>().Ignore(value => value.Category);
            modelBuilder.Entity<Skills>().Ignore(value => value.Aliases);
        }
    }
}
