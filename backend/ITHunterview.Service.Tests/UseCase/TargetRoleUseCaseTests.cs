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
    public class TargetRoleUseCaseTests : IDisposable
    {
        private sealed class TestContext : ITHunterviewContext
        {
            public TestContext(DbContextOptions<ITHunterviewContext> options) : base(options) { }

            protected override void OnModelCreating(ModelBuilder modelBuilder)
            {
                var allowed = new HashSet<Type>
                {
                    typeof(TargetRoleTemplate), typeof(TargetRoleSkill), typeof(SfiaSkill), typeof(SfiaSkillLevel)
                };

                foreach (var entityType in modelBuilder.Model.GetEntityTypes()
                             .Where(t => !allowed.Contains(t.ClrType))
                             .Select(t => t.ClrType)
                             .Distinct()
                             .ToList())
                {
                    modelBuilder.Ignore(entityType);
                }

                modelBuilder.Entity<TargetRoleTemplate>(entity =>
                {
                    entity.HasKey(t => t.Id);
                    entity.HasMany(t => t.RequiredSkills).WithOne(rs => rs.RoleTemplate).HasForeignKey(rs => rs.RoleTemplateId);
                });

                modelBuilder.Entity<TargetRoleSkill>(entity =>
                {
                    entity.HasKey(tr => tr.Id);
                    entity.HasOne(tr => tr.SfiaSkill).WithMany(s => s.TargetRoleSkills).HasForeignKey(tr => tr.SfiaSkillId);
                });

                modelBuilder.Entity<SfiaSkill>(entity =>
                {
                    entity.HasKey(s => s.Id);
                });
            }
        }

        private readonly TestContext _context;
        private readonly TargetRoleUseCase _sut;

        public TargetRoleUseCaseTests()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new TestContext(options);

            _sut = new TargetRoleUseCase(_context);
        }

        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Fact]
        public async Task GetPagedRolesAsync_ReturnsPagedListWithSkills()
        {
            // Arrange
            var sfiaSkillId = Guid.NewGuid();
            var sfiaSkill = new SfiaSkill { Id = sfiaSkillId, SkillCode = "PROG", SkillName = "Programming" };
            var role = new TargetRoleTemplate
            {
                Id = Guid.NewGuid(),
                RoleName = "Backend Developer",
                Description = "Develops server-side applications",
                RequiredSkills = new List<TargetRoleSkill>
                {
                    new TargetRoleSkill { Id = Guid.NewGuid(), SfiaSkillId = sfiaSkillId, TargetLevel = 4, SfiaSkill = sfiaSkill }
                }
            };
            _context.SfiaSkills.Add(sfiaSkill);
            _context.TargetRoleTemplates.Add(role);
            await _context.SaveChangesAsync();

            // Act
            var result = await _sut.GetPagedRolesAsync(1, 10, "Backend");

            // Assert
            result.Should().NotBeNull();
            result.TotalItems.Should().Be(1);
            result.Items.Should().HaveCount(1);
            result.Items[0].RoleName.Should().Be("Backend Developer");
            result.Items[0].RequiredSkills.Should().HaveCount(1);
            result.Items[0].RequiredSkills[0].SkillCode.Should().Be("PROG");
        }

        [Fact]
        public async Task CreateRoleAsync_WhenValid_SavesRoleAndRequiredSkills()
        {
            // Arrange
            var sfiaId = Guid.NewGuid();
            var sfia = new SfiaSkill { Id = sfiaId, SkillCode = "TEST", SkillName = "Testing" };
            _context.SfiaSkills.Add(sfia);
            await _context.SaveChangesAsync();

            var dto = new CreateTargetRoleTemplateDto
            {
                RoleName = "QA Engineer",
                Description = "Assures quality",
                RequiredSkills = new List<CreateTargetRoleSkillDto>
                {
                    new CreateTargetRoleSkillDto { SfiaSkillId = sfiaId, TargetLevel = 3 }
                }
            };

            // Act
            var result = await _sut.CreateRoleAsync(dto);

            // Assert
            result.Should().NotBeNull();
            result.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.RoleName.Should().Be("QA Engineer");

            var saved = await _context.TargetRoleTemplates.Include(r => r.RequiredSkills).FirstOrDefaultAsync(r => r.RoleName == "QA Engineer");
            saved.Should().NotBeNull();
            saved!.RequiredSkills.Should().HaveCount(1);
        }

        [Fact]
        public async Task DeleteRoleAsync_WhenFound_RemovesRoleFromDatabase()
        {
            // Arrange
            var roleId = Guid.NewGuid();
            _context.TargetRoleTemplates.Add(new TargetRoleTemplate
            {
                Id = roleId,
                RoleName = "Temporary Role"
            });
            await _context.SaveChangesAsync();

            // Act
            var result = await _sut.DeleteRoleAsync(roleId);

            // Assert
            result.Success.Should().BeTrue();
            result.Data.Should().BeTrue();
            (await _context.TargetRoleTemplates.AnyAsync(r => r.Id == roleId)).Should().BeFalse();
        }
    }
}
