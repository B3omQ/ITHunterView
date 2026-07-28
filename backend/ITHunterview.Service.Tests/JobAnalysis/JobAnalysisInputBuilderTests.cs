using System;
using System.Collections.Generic;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.Utils;
using Xunit;

namespace ITHunterview.Service.Tests.JobAnalysis
{
    public class JobAnalysisInputBuilderTests
    {
        private readonly JobAnalysisInputBuilder _builder = new();

        [Fact]
        public void Build_NormalizesEvidenceInputAndExcludesMetadata()
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
            Assert.Null(snapshot.JobDomain);
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

        [Fact]
        public void ComputeSemanticHash_WhenTitleChanges_ReturnsDifferentHash()
        {
            var job1 = new JobPostings { Title = "Dev 1", Description = "Desc", Requirements = "Req" };
            var job2 = new JobPostings { Title = "Dev 2", Description = "Desc", Requirements = "Req" };

            var snap1 = _builder.Build(job1);
            var snap2 = _builder.Build(job2);

            var semHash1 = _builder.ComputeSemanticHash(snap1);
            var semHash2 = _builder.ComputeSemanticHash(snap2);

            Assert.NotEqual(semHash1, semHash2);
        }

        [Fact]
        public void ComputeHashes_WhenOnlyCosmeticWhitespaceChanges_ReturnSameValues()
        {
            var systemPrompt = Guid.NewGuid();
            var userPrompt = Guid.NewGuid();
            var job1 = new JobPostings
            {
                Title = " Backend\tEngineer ",
                Description = "Build APIs\r\n\r\n\r\nwith C#",
                Requirements = "C#\u00A0and   PostgreSQL"
            };
            var job2 = new JobPostings
            {
                Title = "Backend Engineer",
                Description = "Build APIs\n\nwith C#",
                Requirements = "C# and PostgreSQL"
            };

            var snapshot1 = _builder.Build(job1);
            var snapshot2 = _builder.Build(job2);

            Assert.Equal(_builder.ComputeSemanticHash(snapshot1), _builder.ComputeSemanticHash(snapshot2));
            Assert.Equal(
                _builder.ComputeAnalysisHash(snapshot1, systemPrompt, userPrompt),
                _builder.ComputeAnalysisHash(snapshot2, systemPrompt, userPrompt));
        }

        [Fact]
        public void ComputeHashes_WhenOnlyMarkdownFormattingChanges_ReturnSameValues()
        {
            var systemPrompt = Guid.NewGuid();
            var userPrompt = Guid.NewGuid();
            var job1 = new JobPostings
            {
                Title = "Backend Engineer",
                Description = "Build APIs with React",
                Requirements = "React\nNode.js"
            };
            var job2 = new JobPostings
            {
                Title = "Backend Engineer",
                Description = "Build APIs with **React**",
                Requirements = "- React\n- _Node.js_"
            };

            var snapshot1 = _builder.Build(job1);
            var snapshot2 = _builder.Build(job2);

            Assert.Equal(snapshot1.Description, snapshot2.Description);
            Assert.Equal(snapshot1.Requirements, snapshot2.Requirements);
            Assert.Equal(_builder.ComputeSemanticHash(snapshot1), _builder.ComputeSemanticHash(snapshot2));
            Assert.Equal(
                _builder.ComputeAnalysisHash(snapshot1, systemPrompt, userPrompt),
                _builder.ComputeAnalysisHash(snapshot2, systemPrompt, userPrompt));
        }

        [Fact]
        public void ComputeHashes_WhenFormattingWrapsAcrossLines_ReturnSameValues()
        {
            var systemPrompt = Guid.NewGuid();
            var userPrompt = Guid.NewGuid();
            var plainJob = new JobPostings
            {
                Title = "Backend Engineer",
                Description = "Own the API\nlifecycle",
                Requirements = "React"
            };
            var formattedJob = new JobPostings
            {
                Title = "Backend Engineer",
                Description = "**Own the API\nlifecycle**",
                Requirements = "React"
            };

            var plainSnapshot = _builder.Build(plainJob);
            var formattedSnapshot = _builder.Build(formattedJob);

            Assert.Equal(plainSnapshot.Description, formattedSnapshot.Description);
            Assert.Equal(_builder.ComputeSemanticHash(plainSnapshot), _builder.ComputeSemanticHash(formattedSnapshot));
            Assert.Equal(
                _builder.ComputeAnalysisHash(plainSnapshot, systemPrompt, userPrompt),
                _builder.ComputeAnalysisHash(formattedSnapshot, systemPrompt, userPrompt));
        }

        [Fact]
        public void ComputeHashes_WhenOnlyContextMetadataChanges_ReturnSameValues()
        {
            var systemPrompt = Guid.NewGuid();
            var userPrompt = Guid.NewGuid();
            var job1 = new JobPostings
            {
                Title = "Backend Dev", Description = "Desc", Requirements = "Req",
                Level = "Junior", WorkingModel = "Remote", JobExpertise = "API", JobDomain = new List<string> { "Fintech" }
            };
            var job2 = new JobPostings
            {
                Title = "Backend Dev", Description = "Desc", Requirements = "Req",
                Level = "Senior", WorkingModel = "Onsite", JobExpertise = "Platform", JobDomain = new List<string> { "Healthcare" }
            };

            var snapshot1 = _builder.Build(job1);
            var snapshot2 = _builder.Build(job2);

            Assert.Equal(_builder.ComputeSemanticHash(snapshot1), _builder.ComputeSemanticHash(snapshot2));
            Assert.Equal(
                _builder.ComputeAnalysisHash(snapshot1, systemPrompt, userPrompt),
                _builder.ComputeAnalysisHash(snapshot2, systemPrompt, userPrompt));
        }
    }
}
