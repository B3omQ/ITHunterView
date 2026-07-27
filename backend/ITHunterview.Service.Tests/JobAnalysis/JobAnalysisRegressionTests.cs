using System;
using System.Collections.Generic;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Job;
using ITHunterview.Service.DTOs.JobAnalysis;
using ITHunterview.Service.Helpers;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis
{
    public class JobAnalysisRegressionTests
    {
        [Fact]
        public void JobPostingDetailDto_CanInstantiateAndAssign()
        {
            var dto = new JobPostingDetailDto
            {
                Id = Guid.NewGuid(),
                Title = "Backend Engineer",
                Description = "Description text",
                Requirements = "Requirements text"
            };

            Assert.Equal("Backend Engineer", dto.Title);
        }

        [Fact]
        public void InputBuilder_NormalizesInputConsistently()
        {
            var builder = new JobAnalysisInputBuilder();
            var job = new JobPostings
            {
                Title = " Senior C# Developer ",
                Description = "Requirements with \r\n CRLF",
                Requirements = "C# & .NET",
                JobDomain = new List<string> { "Fintech", "Cloud" }
            };

            var snapshot = builder.Build(job);

            Assert.Equal("Senior C# Developer", snapshot.Title);
            Assert.Equal("Requirements with\nCRLF", snapshot.Description);
        }

        [Fact]
        public void FinalizeJobRequestDto_CanInstantiate()
        {
            var dto = new FinalizeJobRequestDto
            {
                AnalysisRunId = Guid.NewGuid(),
                ExpectedJobRevision = 1
            };

            Assert.Equal(1, dto.ExpectedJobRevision);
        }
    }
}
