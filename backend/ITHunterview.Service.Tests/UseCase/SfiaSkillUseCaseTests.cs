using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.MasterData;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class SfiaSkillUseCaseTests : IDisposable
    {
        private sealed class TestContext : ITHunterviewContext
        {
            public TestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                var allowed = new HashSet<Type>
                {
                    typeof(SfiaSkill), typeof(SfiaSkillLevel), typeof(TargetRoleSkill)
                };

                foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                             .Where(t => !allowed.Contains(t.ClrType))
                             .Select(t => t.ClrType)
                             .Distinct()
                             .ToList())
                {
                    modelBuilder.Ignore(entityType);
                }

                modelBuilder.Entity<SfiaSkill>(entity =>
                {
                    entity.HasKey(s => s.Id);
                    entity.HasMany(s => s.Levels).WithOne(l => l.SfiaSkill).HasForeignKey(l => l.SfiaSkillId);
                    entity.HasMany(s => s.TargetRoleSkills).WithOne(tr => tr.SfiaSkill).HasForeignKey(tr => tr.SfiaSkillId);
                });

                modelBuilder.Entity<SfiaSkillLevel>(entity =>
                {
                    entity.HasKey(l => l.Id);
                });

                modelBuilder.Entity<TargetRoleSkill>(entity =>
                {
                    entity.HasKey(tr => tr.Id);
                });
            }
        }

        private readonly TestContext _context;
        private readonly SfiaSkillUseCase _sut;

        public SfiaSkillUseCaseTests()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TestContext(options);

            _sut = new SfiaSkillUseCase(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetAllSfiaSkillsAsync_ReturnsFilteredSfiaSkills()
        {
            // Arrange
            _context.SfiaSkills.AddRange(
                new SfiaSkill { Id = Guid.NewGuid(), SkillCode = "PROG", SkillName = "Programming/software development", Category = "Development" },
                new SfiaSkill { Id = Guid.NewGuid(), SkillCode = "TEST", SkillName = "Software testing", Category = "Quality" }
            );
            await _context.SaveChangesAsync();

            // Act
            var result = await _sut.GetAllSfiaSkillsAsync("prog");

            // Assert
            result.Should().HaveCount(1);
            result[0].SkillCode.Should().Be("PROG");
        }

        [Fact]
        public async Task GetSfiaSkillByIdAsync_WhenIdExists_ReturnsDto()
        {
            // Arrange
            var skillId = Guid.NewGuid();
            _context.SfiaSkills.Add(new SfiaSkill
            {
                Id = skillId,
                SkillCode = "ARCH",
                SkillName = "Solution architecture",
                Category = "Strategy",
                Description = "Designing software systems"
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _sut.GetSfiaSkillByIdAsync(skillId);

            // Assert
            result.Should().NotBeNull();
            result.SkillCode.Should().Be("ARCH");
            result.SkillName.Should().Be("Solution architecture");
        }

        [Fact]
        public async Task CreateSfiaSkillAsync_WhenCodeAlreadyExists_ThrowsArgumentException()
        {
            // Arrange
            _context.SfiaSkills.Add(new SfiaSkill { Id = Guid.NewGuid(), SkillCode = "PROG", SkillName = "Programming" });
            await _context.SaveChangesAsync();

            var dto = new CreateSfiaSkillDto { SkillCode = "PROG", SkillName = "Duplicate Prog" };

            // Act
            Func<Task> act = async () => await _sut.CreateSfiaSkillAsync(dto);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>()
                .WithMessage("*SFIA Skill with code 'PROG' already exists*");
        }

        [Fact]
        public async Task CreateSfiaSkillAsync_WhenValid_CreatesSkillAndLevels()
        {
            // Arrange
            var dto = new CreateSfiaSkillDto
            {
                SkillCode = "SEAC",
                SkillName = "Security Administration",
                Category = "Information Security",
                Description = "Managing access controls",
                Levels = new List<CreateSfiaSkillLevelDto>
                {
                    new CreateSfiaSkillLevelDto { Level = 3, Description = "Applies security controls" }
                }
            };

            // Act
            var result = await _sut.CreateSfiaSkillAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.SkillCode.Should().Be("SEAC");
            result.Levels.Should().HaveCount(1);

            var dbItem = await _context.SfiaSkills.Include(s => s.Levels).FirstOrDefaultAsync(s => s.SkillCode == "SEAC");
            dbItem.Should().NotBeNull();
            dbItem!.Levels.Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteSfiaSkillAsync_WhenFound_RemovesFromDatabase()
        {
            // Arrange
            var skillId = Guid.NewGuid();
            _context.SfiaSkills.Add(new SfiaSkill { Id = skillId, SkillCode = "TEMP", SkillName = "Temporary Skill" });
            await _context.SaveChangesAsync();

            // Act
            var success = await _sut.DeleteSfiaSkillAsync(skillId);

            // Assert
            success.Should().BeTrue();
            (await _context.SfiaSkills.AnyAsync(s => s.Id == skillId)).Should().BeFalse();
        }
    }
}
