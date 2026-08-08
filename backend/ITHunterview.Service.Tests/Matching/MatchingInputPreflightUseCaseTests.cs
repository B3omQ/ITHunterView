using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Persistence;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.UseCase;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public class MatchingInputPreflightUseCaseTests
{
    [Fact]
    public async Task PrepareAsync_InvalidShape_ThrowsBeforeAnyRepositoryAccess()
    {
        var validator = new Mock<IMatchingRequestValidator>();
        validator.Setup(v => v.Validate(It.IsAny<MatchingRequestDto>()))
            .Returns(MatchingRequestValidationResult.Failure("MULTIPLE_CV_SOURCES"));
        var repository = new Mock<IMatchingSourceRepository>(MockBehavior.Strict);
        var sut = new MatchingInputPreflightUseCase(validator.Object, repository.Object);

        Func<Task> action = async () => await sut.PrepareAsync(Guid.NewGuid(), new MatchingRequestDto());

        await action.Should().ThrowAsync<ArgumentException>()
            .WithMessage("MULTIPLE_CV_SOURCES*");
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task PrepareAsync_ForeignSavedCv_ThrowsSafeNotFound()
    {
        var candidateId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var repository = new Mock<IMatchingSourceRepository>();
        repository.Setup(r => r.GetOwnedCvAsync(cvId, candidateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Cvs?)null);
        var sut = new MatchingInputPreflightUseCase(
            Validating(new MatchingInputSelection(cvId, null, null, new string('j', 100), "client.pdf", "client JD", MatchingMode.JdFit)),
            repository.Object);

        Func<Task> action = async () => await sut.PrepareAsync(candidateId, new MatchingRequestDto());

        await action.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("CV not found");
    }

    [Fact]
    public async Task PrepareAsync_SavedSources_UsesAuthorizedDatabaseDisplayMetadata()
    {
        var candidateId = Guid.NewGuid();
        var cvId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var repository = new Mock<IMatchingSourceRepository>();
        repository.Setup(r => r.GetOwnedCvAsync(cvId, candidateId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Cvs { Id = cvId, FileName = "trusted-cv.pdf", FileUrl = "url", FileType = "application/pdf" });
        repository.Setup(r => r.GetAccessibleJobAsync(jobId, candidateId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new JobPostings { Id = jobId, Title = "Trusted Job" });
        var sut = new MatchingInputPreflightUseCase(
            Validating(new MatchingInputSelection(cvId, null, jobId, null, "client-name.pdf", "client title", MatchingMode.JdFit)),
            repository.Object);

        var result = await sut.PrepareAsync(candidateId, new MatchingRequestDto());

        result.Cv.Should().Be(new PreparedSavedCvSource(cvId, "trusted-cv.pdf"));
        result.Jd.Should().Be(new PreparedSavedJdSource(jobId, "Trusted Job"));
    }

    [Fact]
    public async Task RecheckAccessAsync_InaccessibleSavedJob_ThrowsSafeNotFound()
    {
        var jobId = Guid.NewGuid();
        var repository = new Mock<IMatchingSourceRepository>();
        repository.Setup(r => r.GetAccessibleJobAsync(jobId, It.IsAny<Guid>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((JobPostings?)null);
        var sut = new MatchingInputPreflightUseCase(Validating(null), repository.Object);
        var prepared = new PreparedMatchingRequest(
            new PreparedRawCvSource(new string('c', 100), null),
            new PreparedSavedJdSource(jobId, "title"),
            MatchingMode.JdFit);

        Func<Task> action = async () => await sut.RecheckAccessAsync(Guid.NewGuid(), prepared);

        await action.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("Job not found");
    }

    private static IMatchingRequestValidator Validating(MatchingInputSelection? selection)
    {
        var validator = new Mock<IMatchingRequestValidator>();
        validator.Setup(v => v.Validate(It.IsAny<MatchingRequestDto>()))
            .Returns(selection is null
                ? MatchingRequestValidationResult.Failure("UNUSED")
                : MatchingRequestValidationResult.Success(selection));
        return validator.Object;
    }
}
