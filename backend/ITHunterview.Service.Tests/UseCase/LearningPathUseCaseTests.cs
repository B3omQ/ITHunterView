using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.LearningPath;
using ITHunterview.Service.Infrastructure.Persistence;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service;
using ITHunterview.Service.UseCase;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace ITHunterview.Service.Tests.UseCase
{
    public class LearningPathUseCaseTests
    {
        private readonly Mock<ILearningPathRepository> _learningPathRepositoryMock;
        private readonly Mock<IInterviewAnswerRepository> _interviewAnswerRepositoryMock;
        private readonly Mock<IInterviewSessionRepository> _interviewSessionRepositoryMock;
        private readonly Mock<IAiService> _aiServiceMock;
        
        // Use Mock for DbContext, but setup DbSets using MockQueryable
        private readonly Mock<ITHunterviewContext> _contextMock;
        
        private readonly LearningPathUseCase _useCase;

        public LearningPathUseCaseTests()
        {
            _learningPathRepositoryMock = new Mock<ILearningPathRepository>();
            _interviewAnswerRepositoryMock = new Mock<IInterviewAnswerRepository>();
            _interviewSessionRepositoryMock = new Mock<IInterviewSessionRepository>();
            _aiServiceMock = new Mock<IAiService>();
            
            // Setup DbContext
            var dbContextOptions = new DbContextOptionsBuilder<ITHunterviewContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _contextMock = new Mock<ITHunterviewContext>(dbContextOptions);

            _useCase = new LearningPathUseCase(
                _learningPathRepositoryMock.Object,
                _interviewAnswerRepositoryMock.Object,
                _interviewSessionRepositoryMock.Object,
                _aiServiceMock.Object,
                _contextMock.Object
            );
        }

        // Helper to setup a DbSet in the context mock
        private void SetupDbSet<TEntity>(List<TEntity> data, Action<Mock<DbSet<TEntity>>>? setupAction = null) where TEntity : class
        {
            var dbSetMock = data.BuildMockDbSet();
            setupAction?.Invoke(dbSetMock);
            
        }

        // =========================================================================
        // GetMyLearningPathsAsync Tests
        // =========================================================================
        [Fact]
        public async Task GetMyLearningPathsAsync_ShouldReturnMappedDtoList()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var learningPaths = new List<LearningPaths>
            {
                new LearningPaths { Id = Guid.NewGuid(), CandidateId = candidateId, Title = "Path 1", Status = "Active", PathData = "{}", CreatedAt = DateTime.UtcNow },
                new LearningPaths { Id = Guid.NewGuid(), CandidateId = candidateId, Title = "Path 2", Status = "Completed", PathData = "{}", CreatedAt = DateTime.UtcNow }
            };

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByCandidateIdAsync(candidateId))
                .ReturnsAsync(learningPaths);

            // Act
            var result = await _useCase.GetMyLearningPathsAsync(candidateId);

            // Assert
            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result[0].Title.Should().Be("Path 1");
            result[1].Title.Should().Be("Path 2");
            _learningPathRepositoryMock.Verify(repo => repo.GetByCandidateIdAsync(candidateId), Times.Once);
        }

        [Fact]
        public async Task GetMyLearningPathsAsync_EmptyList_ShouldReturnEmptyDtoList()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            _learningPathRepositoryMock
                .Setup(repo => repo.GetByCandidateIdAsync(candidateId))
                .ReturnsAsync(new List<LearningPaths>());

            // Act
            var result = await _useCase.GetMyLearningPathsAsync(candidateId);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEmpty();
        }

        // =========================================================================
        // GetLearningPathByIdAsync Tests
        // =========================================================================
        [Fact]
        public async Task GetLearningPathByIdAsync_PathFoundAndCandidateIdMatches_ShouldReturnDto()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            var path = new LearningPaths { Id = pathId, CandidateId = candidateId, Title = "Test Path", Status = "Active", PathData = "{\"key\":\"value\"}", CreatedAt = DateTime.UtcNow };

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByIdAsync(pathId))
                .ReturnsAsync(path);

            // Act
            var result = await _useCase.GetLearningPathByIdAsync(candidateId, pathId);

            // Assert
            result.Should().NotBeNull();
            result.Id.Should().Be(pathId);
            result.Title.Should().Be("Test Path");
            result.PathData.RootElement.GetProperty("key").GetString().Should().Be("value");
        }

        [Fact]
        public async Task GetLearningPathByIdAsync_PathNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByIdAsync(pathId))
                .ReturnsAsync((LearningPaths?)null);

            // Act
            var act = async () => await _useCase.GetLearningPathByIdAsync(candidateId, pathId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Learning path not found.");
        }

        [Fact]
        public async Task GetLearningPathByIdAsync_CandidateIdMismatch_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var requestingCandidateId = Guid.NewGuid();
            var ownerCandidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            var path = new LearningPaths { Id = pathId, CandidateId = ownerCandidateId, Title = "Test Path", Status = "Active", PathData = "{}" };

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByIdAsync(pathId))
                .ReturnsAsync(path);

            // Act
            var act = async () => await _useCase.GetLearningPathByIdAsync(requestingCandidateId, pathId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Learning path not found.");
        }

        // =========================================================================
        // DeleteLearningPathAsync Tests
        // =========================================================================
        [Fact]
        public async Task DeleteLearningPathAsync_PathFoundAndCandidateIdMatches_ShouldCallDeleteAsync()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            var path = new LearningPaths { Id = pathId, CandidateId = candidateId, Title = "To be deleted", Status = "Active", PathData = "{}" };

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByIdAsync(pathId))
                .ReturnsAsync(path);

            // Act
            await _useCase.DeleteLearningPathAsync(candidateId, pathId);

            // Assert
            _learningPathRepositoryMock.Verify(repo => repo.DeleteAsync(path), Times.Once);
        }

        [Fact]
        public async Task DeleteLearningPathAsync_PathNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByIdAsync(pathId))
                .ReturnsAsync((LearningPaths?)null);

            // Act
            var act = async () => await _useCase.DeleteLearningPathAsync(candidateId, pathId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Learning path not found or access denied.");
            _learningPathRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<LearningPaths>()), Times.Never);
        }

        [Fact]
        public async Task DeleteLearningPathAsync_CandidateIdMismatch_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var requestingCandidateId = Guid.NewGuid();
            var ownerCandidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            var path = new LearningPaths { Id = pathId, CandidateId = ownerCandidateId, Title = "Not yours", Status = "Active", PathData = "{}" };

            _learningPathRepositoryMock
                .Setup(repo => repo.GetByIdAsync(pathId))
                .ReturnsAsync(path);

            // Act
            var act = async () => await _useCase.DeleteLearningPathAsync(requestingCandidateId, pathId);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Learning path not found or access denied.");
            _learningPathRepositoryMock.Verify(repo => repo.DeleteAsync(It.IsAny<LearningPaths>()), Times.Never);
        }

        // =========================================================================
        // ExtractFromCvJdAsync Tests
        // =========================================================================
        [Fact]
        public async Task ExtractFromCvJdAsync_CachedResultExists_ShouldReturnCachedResult()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var matchScoreId = Guid.NewGuid();
            var cachedResponse = new ExtractSfiaProfileResponseDto { CustomRoleName = "Cached Role" };
            
            var matchScores = new List<CvJobMatchScores>
            {
                new CvJobMatchScores 
                { 
                    Id = matchScoreId, 
                    UserId = candidateId, 
                    SfiaExtractResult = "{\"customRoleName\": \"Cached Role\"}" 
                }
            };
            SetupDbSet(matchScores, mock => _contextMock.Setup(c => c.CvJobMatchScores).Returns(mock.Object));

            // Act
            var result = await _useCase.ExtractFromCvJdAsync(candidateId, matchScoreId);

            // Assert
            result.Should().NotBeNull();
            result.CustomRoleName.Should().Be("Cached Role");
            _aiServiceMock.Verify(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExtractFromCvJdAsync_CachedResultBelongsToAnotherCandidate_ShouldNotReturnCachedResult()
        {
            var requestingCandidateId = Guid.NewGuid();
            var ownerCandidateId = Guid.NewGuid();
            var matchScoreId = Guid.NewGuid();
            var matchScores = new List<CvJobMatchScores>
            {
                new()
                {
                    Id = matchScoreId,
                    UserId = ownerCandidateId,
                    MatchDetails = "{\"jdFit\":{\"score\":90}}",
                    SfiaExtractResult = "{\"customRoleName\":\"Private cached role\"}"
                }
            };
            SetupDbSet(matchScores, mock => _contextMock.Setup(c => c.CvJobMatchScores).Returns(mock.Object));

            var act = async () => await _useCase.ExtractFromCvJdAsync(requestingCandidateId, matchScoreId);

            await act.Should().ThrowAsync<InvalidOperationException>();
            _aiServiceMock.Verify(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task PreviewHistoryContextAsync_V4Report_ShouldUseTypedReportWithoutLegacyPoolFields()
        {
            var candidateId = Guid.NewGuid();
            var matchScoreId = Guid.NewGuid();
            var matchScores = new List<CvJobMatchScores>
            {
                new()
                {
                    Id = matchScoreId,
                    UserId = candidateId,
                    MatchType = "AI",
                    MatchScore = 81.8m,
                    JdTitle = "Backend Developer",
                    MatchDetails = """
                    {
                      "contract": "jd-matching/v4",
                      "jdFit": {
                        "scorePercent": 81.8,
                        "narrative": "The candidate meets the core Java requirement.",
                        "requirementGroups": [
                          {
                            "groupId": "grp-001",
                            "operator": "all_of",
                            "importance": "must_have",
                            "requirementVerbatim": "Java and Spring Boot are required.",
                            "groupScore": 0.7,
                            "items": [
                              {
                                "itemId": "item-001",
                                "normalizedText": "Java",
                                "score": 0.7,
                                "reasoning": "Used in project Alpha.",
                                "evidence": [
                                  { "quotation": "Built REST APIs with Java", "section": "experience" }
                                ]
                              }
                            ]
                          }
                        ],
                        "criticalGaps": [
                          {
                            "code": "CRITICAL_GAP",
                            "groupId": "grp-002",
                            "requirement": "English TOEIC 600",
                            "reasoning": "No English certificate is present."
                          }
                        ]
                      }
                    }
                    """
                }
            };
            SetupDbSet(matchScores, mock => _contextMock.Setup(c => c.CvJobMatchScores).Returns(mock.Object));

            var result = await _useCase.PreviewHistoryContextAsync(candidateId, "cv-jd", matchScoreId);

            result.ContextPreview.Should().Contain("Overall Match Score: 81.8/100");
            result.ContextPreview.Should().Contain("Java (Score: 0.7): Used in project Alpha.");
            result.ContextPreview.Should().Contain("Evidence [experience]: Built REST APIs with Java");
            result.ContextPreview.Should().Contain("Critical Gaps: English TOEIC 600 - No English certificate is present.");
            result.ContextPreview.Should().NotContain("Technical Skills Score");
            result.ContextPreview.Should().NotContain("Penalty Evidence");
            result.ContextPreview.Should().NotContain("Areas for Improvement");
        }

        [Fact]
        public async Task PreviewHistoryContextAsync_MalformedDetails_ShouldNotExposeRawJson()
        {
            var candidateId = Guid.NewGuid();
            var matchScoreId = Guid.NewGuid();
            const string malformedDetails = "{\"privateCvEvidence\":\"do-not-leak\"";
            var matchScores = new List<CvJobMatchScores>
            {
                new()
                {
                    Id = matchScoreId,
                    UserId = candidateId,
                    MatchScore = 42m,
                    MatchDetails = malformedDetails
                }
            };
            SetupDbSet(matchScores, mock => _contextMock.Setup(c => c.CvJobMatchScores).Returns(mock.Object));

            var result = await _useCase.PreviewHistoryContextAsync(candidateId, "cv-jd", matchScoreId);

            result.ContextPreview.Should().Contain("Overall Match Score: 42.0/100");
            result.ContextPreview.Should().Contain("Matching details are unavailable for this legacy result.");
            result.ContextPreview.Should().NotContain("privateCvEvidence");
            result.ContextPreview.Should().NotContain("do-not-leak");
        }

        [Fact]
        public async Task ExtractFromCvJdAsync_NoCachedResult_ShouldCallAiAndSave()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var matchScoreId = Guid.NewGuid();
            
            var matchScores = new List<CvJobMatchScores>
            {
                new CvJobMatchScores 
                { 
                    Id = matchScoreId, 
                    UserId = candidateId, 
                    MatchDetails = "{\"jdFit\": {\"poolA\": {\"score\": 60}, \"requirementScores\": []}}",
                    JdTitle = "Backend Dev"
                }
            };
            SetupDbSet(matchScores, mock => _contextMock.Setup(c => c.CvJobMatchScores).Returns(mock.Object));
            SetupDbSet(new List<SfiaSkill>(), mock => _contextMock.Setup(c => c.SfiaSkills).Returns(mock.Object)); // Empty skills list for test

            var aiResponse = "{\"customRoleName\": \"New Role\", \"skills\": []}";
            _aiServiceMock
                .Setup(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _useCase.ExtractFromCvJdAsync(candidateId, matchScoreId);

            // Assert
            result.Should().NotBeNull();
            result.CustomRoleName.Should().Be("New Role");
            matchScores[0].SfiaExtractResult.Should().NotBeNullOrEmpty();
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExtractFromCvJdAsync_NoMatchContext_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var matchScoreId = Guid.NewGuid();
            
            var matchScores = new List<CvJobMatchScores>(); // Empty dataset -> no context
            SetupDbSet(matchScores, mock => _contextMock.Setup(c => c.CvJobMatchScores).Returns(mock.Object));

            // Act
            var act = async () => await _useCase.ExtractFromCvJdAsync(candidateId, matchScoreId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Chưa có dữ liệu matching CV-JD.");
        }

        // =========================================================================
        // ExtractFromInterviewAsync Tests
        // =========================================================================
        [Fact]
        public async Task ExtractFromInterviewAsync_CachedResultExists_ShouldReturnCachedResult()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            
            var sessions = new List<InterviewSessions>
            {
                new InterviewSessions 
                { 
                    Id = sessionId, 
                    CandidateId = candidateId, 
                    SfiaExtractResult = "{\"customRoleName\": \"Cached Int Role\"}" 
                }
            };
            SetupDbSet(sessions, mock => _contextMock.Setup(c => c.InterviewSessions).Returns(mock.Object));

            // Act
            var result = await _useCase.ExtractFromInterviewAsync(candidateId, sessionId);

            // Assert
            result.Should().NotBeNull();
            result.CustomRoleName.Should().Be("Cached Int Role");
            _aiServiceMock.Verify(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task ExtractFromInterviewAsync_NoCachedResult_ShouldCallAiAndSave()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            
            var sessions = new List<InterviewSessions>
            {
                new InterviewSessions 
                { 
                    Id = sessionId, 
                    CandidateId = candidateId
                }
            };
            SetupDbSet(sessions, mock => _contextMock.Setup(c => c.InterviewSessions).Returns(mock.Object));
            
            var answers = new List<InterviewAnswers>
            {
                new InterviewAnswers
                {
                    SessionId = sessionId,
                    QuestionText = "Q1",
                    CandidateTranscript = "A1",
                    AiFeedback = "{\"score\": 8}",
                    ScoreTech = 80,
                    ScoreCommunication = 85
                }
            };
            _interviewAnswerRepositoryMock
                .Setup(repo => repo.GetBySessionIdAsync(sessionId))
                .ReturnsAsync(answers);
                
            SetupDbSet(new List<SfiaSkill>(), mock => _contextMock.Setup(c => c.SfiaSkills).Returns(mock.Object)); // Empty skills list for test

            var aiResponse = "{\"customRoleName\": \"New Int Role\", \"skills\": []}";
            _aiServiceMock
                .Setup(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(aiResponse);

            // Act
            var result = await _useCase.ExtractFromInterviewAsync(candidateId, sessionId);

            // Assert
            result.Should().NotBeNull();
            result.CustomRoleName.Should().Be("New Int Role");
            sessions[0].SfiaExtractResult.Should().NotBeNullOrEmpty();
            _contextMock.Verify(c => c.SaveChangesAsync(It.IsAny<System.Threading.CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ExtractFromInterviewAsync_NoAnswers_ShouldThrowInvalidOperationException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var sessionId = Guid.NewGuid();
            
            var sessions = new List<InterviewSessions>
            {
                new InterviewSessions { Id = sessionId, CandidateId = candidateId }
            };
            SetupDbSet(sessions, mock => _contextMock.Setup(c => c.InterviewSessions).Returns(mock.Object));
            
            _interviewAnswerRepositoryMock
                .Setup(repo => repo.GetBySessionIdAsync(sessionId))
                .ReturnsAsync(new List<InterviewAnswers>()); // No answers

            // Act
            var act = async () => await _useCase.ExtractFromInterviewAsync(candidateId, sessionId);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Chưa có dữ liệu phỏng vấn thử.");
        }

        // =========================================================================
        // ToggleTaskCompletionAsync Tests
        // =========================================================================
        [Fact]
        public async Task ToggleTaskCompletionAsync_ValidToggle_ShouldUpdateTaskAndReturnDto()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            var initialJson = "{\"modules\":[{\"completed\":false,\"tasks\":[{\"title\":\"Task 1\",\"completed\":false},{\"title\":\"Task 2\",\"completed\":false}]}]}";
            
            var path = new LearningPaths 
            { 
                Id = pathId, 
                CandidateId = candidateId, 
                PathData = initialJson 
            };

            _learningPathRepositoryMock.Setup(repo => repo.GetByIdAsync(pathId)).ReturnsAsync(path);

            // Act - Complete the first task
            var result = await _useCase.ToggleTaskCompletionAsync(candidateId, pathId, 0, 0);

            // Assert
            result.Should().NotBeNull();
            _learningPathRepositoryMock.Verify(repo => repo.UpdateAsync(path), Times.Once);
            
            var pathData = result.PathData;
            var tasks = pathData.RootElement.GetProperty("modules")[0].GetProperty("tasks");
            tasks[0].GetProperty("completed").GetBoolean().Should().BeTrue();
        }

        [Fact]
        public async Task ToggleTaskCompletionAsync_PathNotFound_ShouldThrowKeyNotFoundException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();

            _learningPathRepositoryMock.Setup(repo => repo.GetByIdAsync(pathId)).ReturnsAsync((LearningPaths?)null);

            // Act
            var act = async () => await _useCase.ToggleTaskCompletionAsync(candidateId, pathId, 0, 0);

            // Assert
            await act.Should().ThrowAsync<KeyNotFoundException>().WithMessage("Learning path not found or access denied.");
        }

        [Fact]
        public async Task ToggleTaskCompletionAsync_PreviousTaskNotCompleted_ShouldThrowArgumentException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            // Task 1 is false, trying to complete Task 2
            var initialJson = "{\"modules\":[{\"completed\":false,\"tasks\":[{\"completed\":false},{\"completed\":false}]}]}";
            
            var path = new LearningPaths { Id = pathId, CandidateId = candidateId, PathData = initialJson };
            _learningPathRepositoryMock.Setup(repo => repo.GetByIdAsync(pathId)).ReturnsAsync(path);

            // Act
            var act = async () => await _useCase.ToggleTaskCompletionAsync(candidateId, pathId, 0, 1);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("You must complete the previous task first.");
        }

        [Fact]
        public async Task ToggleTaskCompletionAsync_UncheckButNextTaskCompleted_ShouldThrowArgumentException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            // Both tasks completed. Trying to uncheck Task 1 while Task 2 is still checked
            var initialJson = "{\"modules\":[{\"completed\":true,\"tasks\":[{\"completed\":true},{\"completed\":true}]}]}";
            
            var path = new LearningPaths { Id = pathId, CandidateId = candidateId, PathData = initialJson };
            _learningPathRepositoryMock.Setup(repo => repo.GetByIdAsync(pathId)).ReturnsAsync(path);

            // Act - Uncheck task 0
            var act = async () => await _useCase.ToggleTaskCompletionAsync(candidateId, pathId, 0, 0);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Cannot uncheck task because the subsequent task is already completed.");
        }

        [Fact]
        public async Task ToggleTaskCompletionAsync_InvalidModuleIndex_ShouldThrowArgumentException()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var pathId = Guid.NewGuid();
            var initialJson = "{\"modules\":[{\"completed\":false,\"tasks\":[{\"completed\":false}]}]}";
            
            var path = new LearningPaths { Id = pathId, CandidateId = candidateId, PathData = initialJson };
            _learningPathRepositoryMock.Setup(repo => repo.GetByIdAsync(pathId)).ReturnsAsync(path);

            // Act
            var act = async () => await _useCase.ToggleTaskCompletionAsync(candidateId, pathId, 99, 0);

            // Assert
            await act.Should().ThrowAsync<ArgumentException>().WithMessage("Invalid module index.");
        }

        // =========================================================================
        // GenerateLearningPathAsync Tests
        // =========================================================================
        [Fact]
        public async Task GenerateLearningPathAsync_WithTemplate_ShouldGenerateAndSave()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var templateId = Guid.NewGuid();
            var request = new GeneratePathRequestDto
            {
                TargetRoleTemplateId = templateId,
                CurrentSkills = new List<CandidateSfiaSkillDto> { new CandidateSfiaSkillDto { SkillCode = "DEV", CurrentLevel = 2 } }
            };

            var templates = new List<TargetRoleTemplate>
            {
                new TargetRoleTemplate
                {
                    Id = templateId,
                    RoleName = "Backend",
                    RequiredSkills = new List<TargetRoleSkill>
                    {
                        new TargetRoleSkill { SfiaSkill = new SfiaSkill { SkillCode = "DEV" }, TargetLevel = 4 }
                    }
                }
            };
            SetupDbSet(templates, mock => _contextMock.Setup(c => c.TargetRoleTemplates).Returns(mock.Object));
            
            var subscriptions = new List<UserSubscriptions>();
            SetupDbSet(subscriptions, mock => _contextMock.Setup(c => c.UserSubscriptions).Returns(mock.Object));

            var aiResponse = "{\"title\": \"My Path\", \"modules\": []}";
            _aiServiceMock.Setup(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(aiResponse);
            
            _learningPathRepositoryMock.Setup(repo => repo.GetByCandidateIdAsync(candidateId)).ReturnsAsync(new List<LearningPaths>());

            // Act
            var result = await _useCase.GenerateLearningPathAsync(candidateId, request);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("My Path");
            _learningPathRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<LearningPaths>()), Times.Once);
        }

        [Fact]
        public async Task GenerateLearningPathAsync_WithCustomRole_ShouldGenerateAndSave()
        {
            // Arrange
            var candidateId = Guid.NewGuid();
            var request = new GeneratePathRequestDto
            {
                CustomTargetRoleName = "Custom Role",
                CustomTargetSkills = new List<CustomSfiaSkillDto>
                {
                    new CustomSfiaSkillDto { SkillCode = "TEST", CurrentLevel = 1, TargetLevel = 3 }
                }
            };

            var subscriptions = new List<UserSubscriptions>();
            SetupDbSet(subscriptions, mock => _contextMock.Setup(c => c.UserSubscriptions).Returns(mock.Object));
            
            var skills = new List<SfiaSkill>();
            SetupDbSet(skills, mock => _contextMock.Setup(c => c.SfiaSkills).Returns(mock.Object));

            var aiResponse = "{\"title\": \"Custom Path\", \"modules\": []}";
            _aiServiceMock.Setup(a => a.GenerateTextAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(aiResponse);
            
            _learningPathRepositoryMock.Setup(repo => repo.GetByCandidateIdAsync(candidateId)).ReturnsAsync(new List<LearningPaths>());

            // Act
            var result = await _useCase.GenerateLearningPathAsync(candidateId, request);

            // Assert
            result.Should().NotBeNull();
            result.Title.Should().Be("Custom Path");
            _learningPathRepositoryMock.Verify(repo => repo.AddAsync(It.IsAny<LearningPaths>()), Times.Once);
        }
    }
}
