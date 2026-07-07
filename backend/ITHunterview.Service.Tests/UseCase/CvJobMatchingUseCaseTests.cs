using System;
using System.Text.Json;
using System.Threading.Tasks;
using ITHunterview.Service.Implementations.UseCase;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.Infrastructure.Persistence;
using Moq;
using Pgvector;
using Xunit;
using FluentAssertions;

namespace ITHunterview.Service.Tests.UseCase
{
    public class CvJobMatchingUseCaseTests
    {
        private readonly Mock<IAiEmbeddingService> _mockAiService;
        private readonly CvJobMatchingUseCase _sut;

        public CvJobMatchingUseCaseTests()
        {
            _mockAiService = new Mock<IAiEmbeddingService>();
            
            // Pass null for context since we only test methods that don't hit DB
            _sut = new CvJobMatchingUseCase(null!, _mockAiService.Object);
        }

        [Theory]
        [InlineData("{\"skills\": \"C#, SQL\"}", "skills", "C#, SQL")]
        [InlineData("{\"position\": {\"title\": \"Backend Dev\"}}", "position.title", "Backend Dev")]
        [InlineData("{\"invalid\": json", "skills", "")]
        [InlineData(null, "skills", "")]
        [InlineData("", "skills", "")]
        [InlineData("{\"position\": {\"company\": \"XYZ\"}}", "position.title", "")]
        public void ExtractJsonField_ShouldReturnCorrectValue_BasedOnPath(string? json, string path, string expected)
        {
            // Act
            var result = _sut.ExtractJsonField(json, path);

            // Assert
            result.Should().Be(expected);
        }

        [Fact]
        public void ExtractJsonField_ShouldExtractComplexArrayAsString()
        {
            // Arrange
            var json = @"{
                ""tech_requirements"": {
                    ""must_have"": [
                        { ""skill"": ""Java"" },
                        { ""skill"": ""Spring"" }
                    ]
                }
            }";

            // Act
            var result = _sut.ExtractJsonField(json, "tech_requirements.must_have");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("Java");
            result.Should().Contain("Spring");
        }

        [Fact]
        public void CalculateComponentScore_ShouldReturnZero_WhenVectorsAreNull()
        {
            // Act
            var score1 = _sut.CalculateComponentScore(null, new Vector(new float[] { 1, 0, 0 }));
            var score2 = _sut.CalculateComponentScore(new Vector(new float[] { 1, 0, 0 }), null);
            var score3 = _sut.CalculateComponentScore(null, null);

            // Assert
            score1.Should().Be(0m);
            score2.Should().Be(0m);
            score3.Should().Be(0m);
        }
    }
}
