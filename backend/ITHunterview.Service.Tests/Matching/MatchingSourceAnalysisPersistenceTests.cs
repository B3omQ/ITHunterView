using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingSourceAnalysisPersistenceTests
{
    [Fact]
    public async Task TryPersistCvAsync_UnchangedOwnedSource_PersistsAnalysisAndClearsEmbeddings()
    {
        await using var context = CreateContext();
        var cv = CreateCv();
        context.Cvs.Add(cv);
        await context.SaveChangesAsync();
        var service = new MatchingSourceAnalysisPersistence(context, new JobAnalysisInputBuilder());
        var intent = new CvAnalysisPersistenceIntent(
            cv.Id,
            cv.UserId,
            MatchingSourceFingerprint.ForCv(cv.FileUrl, cv.RawText),
            MatchingSourceFingerprint.ForAnalysis(cv.ParsedData),
            "{\"schema_version\":\"cv-analysis/v3\"}",
            CvAnalysisQuality.PARTIAL,
            "{\"state\":\"partial\"}",
            "[]");

        var outcome = await service.TryPersistCvAsync(intent);

        Assert.Equal(MatchingSourcePersistenceOutcome.Persisted, outcome);
        Assert.Equal(intent.CanonicalJson, cv.ParsedData);
        Assert.Equal("SUCCESS", cv.ParseStatus);
        Assert.Equal(CvAnalysisQuality.PARTIAL, cv.AnalysisQuality);
    }

    [Fact]
    public async Task TryPersistCvAsync_SourceChanged_DoesNotOverwriteNewerSource()
    {
        await using var context = CreateContext();
        var cv = CreateCv();
        context.Cvs.Add(cv);
        await context.SaveChangesAsync();
        var expectedSourceHash = MatchingSourceFingerprint.ForCv(cv.FileUrl, cv.RawText);
        var expectedAnalysisHash = MatchingSourceFingerprint.ForAnalysis(cv.ParsedData);
        cv.RawText = "newer source text";
        await context.SaveChangesAsync();
        var service = new MatchingSourceAnalysisPersistence(context, new JobAnalysisInputBuilder());
        var intent = new CvAnalysisPersistenceIntent(
            cv.Id, cv.UserId, expectedSourceHash, expectedAnalysisHash,
            "{\"schema_version\":\"cv-analysis/v3\"}", CvAnalysisQuality.COMPLETE, null, null);

        var outcome = await service.TryPersistCvAsync(intent);

        Assert.Equal(MatchingSourcePersistenceOutcome.SourceChanged, outcome);
        Assert.Equal("old parsed data", cv.ParsedData);
        Assert.Equal("newer source text", cv.RawText);
    }

    private static Cvs CreateCv() => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        FileUrl = "https://res.cloudinary.com/example/raw/upload/cv.pdf",
        FileName = "cv.pdf",
        FileType = "application/pdf",
        FileSize = 100,
        RawText = "original source text",
        ParsedData = "old parsed data",
        ParseStatus = "SUCCESS",
        AnalysisQuality = CvAnalysisQuality.COMPLETE,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    private static ITHunterviewContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;
        return new PersistenceTestContext(options);
    }

    private sealed class PersistenceTestContext : ITHunterviewContext
    {
        public PersistenceTestContext(DbContextOptions<ITHunterviewContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(Cvs))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
        }
    }
}
