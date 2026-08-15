using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Service;
using ITHunterview.Service.Utils;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis;

public sealed class SkillNormalizationTests
{
    [Theory]
    [InlineData(".NET", ".net")]
    [InlineData(".Net", ".net")]
    [InlineData("c#", "c#")]
    [InlineData("C#", "c#")]
    [InlineData("C++", "c++")]
    [InlineData("CI/CD", "ci/cd")]
    [InlineData("ci/cd", "ci/cd")]
    [InlineData("UI/UX", "ui/ux")]
    [InlineData("PL/SQL", "pl/sql")]
    [InlineData("Node.js", "node.js")]
    [InlineData("Vue.js", "vue.js")]
    [InlineData("Vue.Js", "vue.js")]
    [InlineData("Spring Boot", "spring boot")]
    [InlineData("Problem Solving", "problem solving")]
    [InlineData("JavaScript", "javascript")]
    [InlineData("PostgreSQL", "postgresql")]
    [InlineData("  .NET   Core  ", ".net core")]
    [InlineData("ASP.NET Core", "asp.net core")]
    [InlineData("R&D", "r&d")]
    [InlineData("front-end", "front-end")]
    public void NormalizeITTerm_PreservesITSpecificCharacters(string input, string expected)
    {
        var result = StringNormalizationHelper.NormalizeITTerm(input);
        Assert.Equal(expected, result);

        var service = new SkillNormalizationService();
        var serviceResult = service.Normalize(input);
        Assert.Equal(expected, serviceResult);
    }

    [Fact]
    public async Task SkillResolver_MatchesSpecialITSkills_ExactCanonical()
    {
        var options = new DbContextOptionsBuilder<ITHunterviewContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        using var context = new SkillTestContext(options);
        context.Skills.AddRange(
            new Skills { Id = 12, Name = ".NET", NormalizedName = ".net", Status = SkillStatus.ACTIVE },
            new Skills { Id = 6, Name = "C#", NormalizedName = "c#", Status = SkillStatus.ACTIVE },
            new Skills { Id = 9, Name = "Node.js", NormalizedName = "node.js", Status = SkillStatus.ACTIVE },
            new Skills { Id = 13, Name = "Vue.js", NormalizedName = "vue.js", Status = SkillStatus.ACTIVE },
            new Skills { Id = 24, Name = "CI/CD", NormalizedName = "ci/cd", Status = SkillStatus.ACTIVE }
        );
        await context.SaveChangesAsync();

        var resolver = new SkillResolver(context, new SkillNormalizationService());

        var mentions = new List<ValidatedSkillMention>
        {
            new() { Name = ".net", RawMention = ".NET", Category = "tech_skill", Importance = "must_have" },
            new() { Name = "C#", RawMention = "C#", Category = "tech_skill", Importance = "must_have" },
            new() { Name = "Vue.Js", RawMention = "Vue.Js", Category = "tech_skill", Importance = "nice_to_have" },
            new() { Name = "CI/CD", RawMention = "CI/CD", Category = "tech_skill", Importance = "nice_to_have" }
        };

        var resolutions = await resolver.ResolveAsync(mentions);

        Assert.Equal(4, resolutions.Count);

        var dotNet = Assert.Single(resolutions, r => r.RawMention == ".NET");
        Assert.Equal(SkillResolutionStatus.EXACT_CANONICAL, dotNet.ResolutionStatus);
        Assert.Equal(12, dotNet.ResolvedSkillId);
        Assert.Equal(".NET", dotNet.ResolvedSkillName);

        var csharp = Assert.Single(resolutions, r => r.RawMention == "C#");
        Assert.Equal(SkillResolutionStatus.EXACT_CANONICAL, csharp.ResolutionStatus);
        Assert.Equal(6, csharp.ResolvedSkillId);
        Assert.Equal("C#", csharp.ResolvedSkillName);

        var vue = Assert.Single(resolutions, r => r.RawMention == "Vue.Js");
        Assert.Equal(SkillResolutionStatus.EXACT_CANONICAL, vue.ResolutionStatus);
        Assert.Equal(13, vue.ResolvedSkillId);
        Assert.Equal("Vue.js", vue.ResolvedSkillName);

        var cicd = Assert.Single(resolutions, r => r.RawMention == "CI/CD");
        Assert.Equal(SkillResolutionStatus.EXACT_CANONICAL, cicd.ResolutionStatus);
        Assert.Equal(24, cicd.ResolvedSkillId);
        Assert.Equal("CI/CD", cicd.ResolvedSkillName);
    }

    private sealed class SkillTestContext : ITHunterviewContext
    {
        public SkillTestContext(DbContextOptions<ITHunterviewContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            var allowed = new HashSet<Type>
            {
                typeof(Skills),
                typeof(SkillAliases),
                typeof(SkillCategories)
            };

            foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                         .Where(type => !allowed.Contains(type.ClrType))
                         .Select(type => type.ClrType)
                         .Distinct()
                         .ToList())
            {
                modelBuilder.Ignore(entityType);
            }

            modelBuilder.Entity<Skills>(entity =>
            {
                entity.Ignore(skill => skill.Category);
            });
        }
    }
}
