using System.Linq;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class JobAnalysisQualityRuntimeModelTests
{
    [Fact]
    public void RuntimeModel_MapsJdQualityMetadataForRunsAndMatchRows()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(nameof(RuntimeModel_MapsJdQualityMetadataForRunsAndMatchRows))
            .Options;

        using var context = new MatchOnlyContext(options);
        var match = context.Model.FindEntityType(typeof(CvJobMatchScores));

        typeof(JobAnalysisRuns).GetProperty(nameof(JobAnalysisRuns.AnalysisQuality))
            .Should().NotBeNull();
        typeof(JobAnalysisRuns).GetProperty(nameof(JobAnalysisRuns.AnalysisCoverageJson))
            .Should().NotBeNull();
        typeof(JobAnalysisRuns).GetProperty(nameof(JobAnalysisRuns.AnalysisDiagnosticsJson))
            .Should().NotBeNull();
        match.Should().NotBeNull();
        match!.FindProperty(nameof(CvJobMatchScores.JdAnalysisQuality)).Should().NotBeNull();
        match.FindProperty(nameof(CvJobMatchScores.JdAnalysisCoverageJson)).Should().NotBeNull();
        match.FindProperty(nameof(CvJobMatchScores.JdAnalysisDiagnosticsJson)).Should().NotBeNull();
    }

    private sealed class MatchOnlyContext : ITHunterviewContext
    {
        public MatchOnlyContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => type.ClrType != typeof(CvJobMatchScores))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }
        }
    }
}
