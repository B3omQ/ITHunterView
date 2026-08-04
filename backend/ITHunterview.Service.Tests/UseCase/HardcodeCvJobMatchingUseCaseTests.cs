using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Domain.Enums;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;
using FluentAssertions;

namespace ITHunterview.Service.Tests.UseCase
{
    public class HardcodeCvJobMatchingUseCaseTests
    {
        private sealed class HardcodeMatchingTestContext : ITHunterviewContext
        {
            public HardcodeMatchingTestContext(DbContextOptions<ITHunterviewContext> options)
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

        private HardcodeMatchingTestContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            return new HardcodeMatchingTestContext(options);
        }

        [Fact]
        public async Task MatchJobWithAllCvsHardcodeAsync_ShouldCreateMatchScores_WhenJobAndCvsExist()
        {
            // Arrange
            await using var context = CreateContext();
            var jobId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            var candidateUserId = Guid.NewGuid();
            var recruiterId = Guid.NewGuid();

            var candidateUser = new User
            {
                Id = candidateUserId,
                Email = "candidate@example.com",
                PasswordHash = "hash",
                CandidateProfile = new CandidateProfiles
                {
                    UserId = candidateUserId,
                    IsVisibleToRecruiters = true
                }
            };
            context.Users.Add(candidateUser);

            context.JobPostings.Add(new JobPostings
            {
                Id = jobId,
                Title = "Senior Backend Engineer",
                Description = "Looking for C# .NET Developer with 3+ years experience in SQL and Microservices",
                Requirements = "Must know C#, SQL, EF Core",
                RecruiterId = recruiterId,
                JobCode = "JOB001",
                Benefits = "[]",
                Currency = "VND",
                Location = "Hanoi",
                ParseStatus = "SUCCESS",
                Status = JobStatus.PUBLISHED,
                ParsedData = "{\"position\":{\"title\":\"Backend Engineer\"},\"tech_requirements\":{\"must_have\":[{\"skill\":\"C#\"},{\"skill\":\"SQL\"}]}}"
            });

            context.Cvs.Add(new Cvs
            {
                Id = cvId,
                FileName = "cv_test.pdf",
                RawText = "C# .NET Core, SQL Server, Entity Framework, 4 years experience",
                UserId = candidateUserId,
                FileType = "pdf",
                FileUrl = "https://storage.local/cv_test.pdf",
                ParseStatus = "SUCCESS",
                IsPrimary = true,
                ParsedData = "{\"personal_information\":{\"target_position\":\"Backend Engineer\"},\"skills\":{\"technical_skills\":[\"C#\",\"SQL\"]}}"
            });

            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var logger = NullLogger<HardcodeCvJobMatchingUseCase>.Instance;

            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, logger, scoringService);

            // Act
            await sut.MatchJobWithAllCvsHardcodeAsync(jobId, recruiterId);

            // Assert
            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().NotBeEmpty();
            scores[0].JobId.Should().Be(jobId);
            scores[0].CvId.Should().Be(cvId);
            scores[0].MatchScore.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task MatchJobWithAllCvsHardcodeAsync_ShouldThrow_WhenJobNotFound()
        {
            // Arrange
            await using var context = CreateContext();
            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act & Assert
            var act = () => sut.MatchJobWithAllCvsHardcodeAsync(Guid.NewGuid(), Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("Job not found");
        }

        [Fact]
        public async Task MatchJobWithAllCvsHardcodeAsync_ShouldThrow_WhenJobParseStatusIsNotSuccess()
        {
            // Arrange
            await using var context = CreateContext();
            var jobId = Guid.NewGuid();
            context.JobPostings.Add(new JobPostings
            {
                Id = jobId,
                Title = "Draft Pending Job",
                Description = "Draft Description",
                Requirements = "Draft Requirements",
                JobCode = "JOB003",
                Benefits = "[]",
                Currency = "VND",
                Location = "Hanoi",
                ParseStatus = "PENDING",
                Status = JobStatus.DRAFT
            });
            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act & Assert
            var act = () => sut.MatchJobWithAllCvsHardcodeAsync(jobId, Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("*Job posting is currently in status 'PENDING'*");
        }

        [Fact]
        public async Task MatchJobWithAllCvsHardcodeAsync_ShouldIgnoreHiddenOrNonPrimaryCvs()
        {
            // Arrange
            await using var context = CreateContext();
            var jobId = Guid.NewGuid();
            var candidateUserId = Guid.NewGuid();
            var recruiterId = Guid.NewGuid();

            var candidateUser = new User
            {
                Id = candidateUserId,
                Email = "hidden_candidate@example.com",
                PasswordHash = "hash",
                CandidateProfile = new CandidateProfiles
                {
                    UserId = candidateUserId,
                    IsVisibleToRecruiters = false // Hidden profile
                }
            };
            context.Users.Add(candidateUser);

            context.JobPostings.Add(new JobPostings
            {
                Id = jobId,
                Title = "DevOps Engineer",
                Description = "DevOps Description",
                Requirements = "Docker, K8s",
                JobCode = "JOB004",
                Benefits = "[]",
                Currency = "USD",
                Location = "Remote",
                ParseStatus = "SUCCESS",
                Status = JobStatus.PUBLISHED,
                ParsedData = "{\"position\":{\"title\":\"DevOps Engineer\"}}"
            });

            context.Cvs.Add(new Cvs
            {
                Id = Guid.NewGuid(),
                FileName = "hidden_cv.pdf",
                UserId = candidateUserId,
                FileType = "pdf",
                FileUrl = "https://storage.local/hidden_cv.pdf",
                ParseStatus = "SUCCESS",
                IsPrimary = true,
                ParsedData = "{\"position\":{\"title\":\"DevOps Engineer\"}}"
            });

            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act
            await sut.MatchJobWithAllCvsHardcodeAsync(jobId, recruiterId);

            // Assert
            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().BeEmpty("CV belongs to a candidate with IsVisibleToRecruiters = false");
        }

        [Fact]
        public async Task MatchCvWithAllJobsHardcodeAsync_ShouldCalculateScoreForCandidate()
        {
            // Arrange
            await using var context = CreateContext();
            var jobId = Guid.NewGuid();
            var cvId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();

            context.JobPostings.Add(new JobPostings
            {
                Id = jobId,
                Title = "Frontend Developer",
                Description = "React TypeScript Tailwind CSS Specialist needed",
                Requirements = "React, TypeScript",
                RecruiterId = Guid.NewGuid(),
                JobCode = "JOB002",
                Benefits = "[]",
                Currency = "VND",
                Location = "Hanoi",
                ParseStatus = "SUCCESS",
                Status = JobStatus.PUBLISHED,
                ParsedData = "{\"position\":{\"title\":\"Frontend Developer\"},\"tech_requirements\":{\"must_have\":[{\"skill\":\"React\"},{\"skill\":\"TypeScript\"}]}}"
            });

            context.Cvs.Add(new Cvs
            {
                Id = cvId,
                FileName = "frontend_cv.pdf",
                RawText = "React, TypeScript, Next.js, 3 years experience",
                UserId = candidateId,
                FileType = "pdf",
                FileUrl = "https://storage.local/frontend_cv.pdf",
                ParseStatus = "SUCCESS",
                IsPrimary = true,
                ParsedData = "{\"personal_information\":{\"target_position\":\"Frontend Developer\"},\"skills\":{\"technical_skills\":[\"React\",\"TypeScript\"]}}"
            });

            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var logger = NullLogger<HardcodeCvJobMatchingUseCase>.Instance;

            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, logger, scoringService);

            // Act
            await sut.MatchCvWithAllJobsHardcodeAsync(cvId, candidateId);

            // Assert
            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().NotBeEmpty();
            scores[0].CvId.Should().Be(cvId);
            scores[0].MatchScore.Should().BeGreaterThan(0);
        }

        [Fact]
        public async Task MatchCvWithAllJobsHardcodeAsync_ShouldThrow_WhenCvNotFound()
        {
            // Arrange
            await using var context = CreateContext();
            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act & Assert
            var act = () => sut.MatchCvWithAllJobsHardcodeAsync(Guid.NewGuid(), Guid.NewGuid());
            await act.Should().ThrowAsync<Exception>().WithMessage("CV not found");
        }

        [Fact]
        public async Task MatchCvWithAllJobsHardcodeAsync_ShouldParseCvOnDemand_WhenParseStatusNotSuccess()
        {
            // Arrange
            await using var context = CreateContext();
            var cvId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();
            var jobId = Guid.NewGuid();

            context.Cvs.Add(new Cvs
            {
                Id = cvId,
                FileName = "raw_cv.pdf",
                RawText = "Java Spring Boot Developer",
                UserId = candidateId,
                FileType = "pdf",
                FileUrl = "https://storage.local/raw_cv.pdf",
                ParseStatus = "PENDING",
                ParsedData = "{}"
            });

            context.JobPostings.Add(new JobPostings
            {
                Id = jobId,
                Title = "Java Developer",
                Description = "Java Description",
                Requirements = "Java",
                JobCode = "JOB005",
                Benefits = "[]",
                Currency = "VND",
                Location = "Hanoi",
                ParseStatus = "SUCCESS",
                Status = JobStatus.PUBLISHED,
                ParsedData = "{\"position\":{\"title\":\"Java Developer\"},\"skills\":[\"Java\"]}"
            });

            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            mockExtractor
                .Setup(x => x.ExtractParsedDataFromUrlAsync("https://storage.local/raw_cv.pdf", "Java Spring Boot Developer"))
                .ReturnsAsync("{\"personal_information\":{\"target_position\":\"Java Developer\"},\"skills\":{\"technical_skills\":[\"Java\"]}}");

            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act
            await sut.MatchCvWithAllJobsHardcodeAsync(cvId, candidateId);

            // Assert
            var updatedCv = await context.Cvs.FindAsync(cvId);
            updatedCv.Should().NotBeNull();
            updatedCv!.ParseStatus.Should().Be("SUCCESS");
            updatedCv.ParsedData.Should().NotBeNullOrEmpty();

            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().NotBeEmpty();
        }

        [Fact]
        public async Task MatchCvWithAllJobsHardcodeAsync_ShouldThrow_WhenOnDemandParseFails()
        {
            // Arrange
            await using var context = CreateContext();
            var cvId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();

            context.Cvs.Add(new Cvs
            {
                Id = cvId,
                FileName = "corrupted_cv.pdf",
                RawText = "Broken text",
                UserId = candidateId,
                FileType = "pdf",
                FileUrl = "https://storage.local/corrupted_cv.pdf",
                ParseStatus = "PENDING",
                ParsedData = "{}"
            });

            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            mockExtractor
                .Setup(x => x.ExtractParsedDataFromUrlAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync((string?)null); // Thất bại

            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act & Assert
            var act = () => sut.MatchCvWithAllJobsHardcodeAsync(cvId, candidateId);
            await act.Should().ThrowAsync<Exception>().WithMessage("*Cannot parse CV data on-demand*");

            var updatedCv = await context.Cvs.FindAsync(cvId);
            updatedCv!.ParseStatus.Should().Be("FAILED");
        }

        [Fact]
        public async Task MatchCvWithAllJobsHardcodeAsync_ShouldSkipNonPublishedJobs()
        {
            // Arrange
            await using var context = CreateContext();
            var cvId = Guid.NewGuid();
            var candidateId = Guid.NewGuid();

            context.Cvs.Add(new Cvs
            {
                Id = cvId,
                FileName = "valid_cv.pdf",
                UserId = candidateId,
                FileType = "pdf",
                FileUrl = "https://storage.local/valid_cv.pdf",
                ParseStatus = "SUCCESS",
                ParsedData = "{\"position\":{\"title\":\"Python Dev\"}}"
            });

            context.JobPostings.Add(new JobPostings
            {
                Id = Guid.NewGuid(),
                Title = "Draft Python Job",
                Description = "Python Description",
                Requirements = "Python",
                JobCode = "JOB006",
                Benefits = "[]",
                Currency = "USD",
                Location = "Remote",
                ParseStatus = "SUCCESS",
                Status = JobStatus.DRAFT // DRAFT status -> Should be skipped!
            });

            await context.SaveChangesAsync();

            var mockExtractor = new Mock<ICvTextExtractorService>();
            var scoringService = new HardcodeJdRequirementScoringService(new JdRequirementProjector(), new JdHardcodeRequirementEvaluator());
            var sut = new HardcodeCvJobMatchingUseCase(context, mockExtractor.Object, NullLogger<HardcodeCvJobMatchingUseCase>.Instance, scoringService);

            // Act
            await sut.MatchCvWithAllJobsHardcodeAsync(cvId, candidateId);

            // Assert
            var scores = await context.CvJobMatchScores.ToListAsync();
            scores.Should().BeEmpty("Job posting is in DRAFT status, not PUBLISHED");
        }
    }
}


