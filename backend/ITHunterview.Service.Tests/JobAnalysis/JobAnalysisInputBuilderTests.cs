using System;
using System.Collections.Generic;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Helpers;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis
{
    public class JobAnalysisInputBuilderTests
    {
        private readonly JobAnalysisInputBuilder _builder = new();

        [Fact]
        public void Build_NormalizesNewlinesAndSortsDomains()
        {
            var job = new JobPostings
            {
                Title = " Senior Backend Engineer ",
                Description = "Line1\r\nLine2\rLine3",
                Requirements = "Must have C#",
                JobDomain = new List<string> { "Fintech", " E-Commerce ", "fintech" }
            };

            var snapshot = _builder.Build(job);

            Assert.Equal("Senior Backend Engineer", snapshot.Title);
            Assert.Equal("Line1\nLine2\nLine3", snapshot.Description);
            Assert.NotNull(snapshot.JobDomain);
            Assert.Equal(2, snapshot.JobDomain!.Count);
            Assert.Equal("E-Commerce", snapshot.JobDomain[0]);
            Assert.Equal("Fintech", snapshot.JobDomain[1]);
        }

        [Fact]
        public void ComputeHash_WhenOnlyBenefitsChanges_ReturnsSameHash()
        {
            var sysPromptId = Guid.NewGuid();
            var userPromptId = Guid.NewGuid();

            var job1 = new JobPostings
            {
                Title = "Backend Dev",
                Description = "Desc",
                Requirements = "Req",
                Benefits = "Benefit 1"
            };

            var job2 = new JobPostings
            {
                Title = "Backend Dev",
                Description = "Desc",
                Requirements = "Req",
                Benefits = "Benefit 2 completely different"
            };

            var snap1 = _builder.Build(job1);
            var snap2 = _builder.Build(job2);

            var hash1 = _builder.ComputeHash(snap1, sysPromptId, userPromptId);
            var hash2 = _builder.ComputeHash(snap2, sysPromptId, userPromptId);

            Assert.Equal(hash1, hash2);
        }

        [Fact]
        public void ComputeHash_WhenRequirementsChanges_ReturnsDifferentHash()
        {
            var sysPromptId = Guid.NewGuid();
            var userPromptId = Guid.NewGuid();

            var job1 = new JobPostings { Title = "Dev", Description = "Desc", Requirements = "Req 1" };
            var job2 = new JobPostings { Title = "Dev", Description = "Desc", Requirements = "Req 2" };

            var snap1 = _builder.Build(job1);
            var snap2 = _builder.Build(job2);

            var hash1 = _builder.ComputeHash(snap1, sysPromptId, userPromptId);
            var hash2 = _builder.ComputeHash(snap2, sysPromptId, userPromptId);

            Assert.NotEqual(hash1, hash2);
        }
    }
}
