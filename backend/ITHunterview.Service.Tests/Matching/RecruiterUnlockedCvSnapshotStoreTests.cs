using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using ITHunterview.Service.Service.Matching;
using Microsoft.Extensions.Logging;
using Moq;

namespace ITHunterview.Service.Tests.Matching;

public sealed class RecruiterUnlockedCvSnapshotStoreTests
{
    private readonly List<string> _logEntries = new();
    private readonly Mock<ILogger<CloudinaryRecruiterUnlockedCvSnapshotStore>> _loggerMock = new();
    private readonly Mock<ICloudinaryStorageClient> _storageClientMock = new(MockBehavior.Strict);

    public RecruiterUnlockedCvSnapshotStoreTests()
    {
        _loggerMock
            .Setup(l => l.Log(
                It.IsAny<LogLevel>(),
                It.IsAny<EventId>(),
                It.IsAny<It.IsAnyType>(),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()))
            .Callback(new InvocationAction(invocation =>
            {
                var state = invocation.Arguments[2];
                var exception = (Exception?)invocation.Arguments[3];
                var formatter = invocation.Arguments[4];
                var message = formatter?.GetType().GetMethod("Invoke")?.Invoke(formatter, new[] { state, exception })?.ToString()
                              ?? state?.ToString()
                              ?? string.Empty;
                _logEntries.Add(message);
            }));
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task CaptureAsync_SameUnlockTwice_ReturnsSameImmutableStorageKey()
    {
        var unlockId = Guid.NewGuid();
        var cv = CreateCv("candidate_cv.pdf", "https://res.cloudinary.com/demo/raw/upload/v1/cv/candidate_cv.pdf");
        var fileBytes = Encoding.UTF8.GetBytes("PDF-1.4 mock cv file content");
        var expectedHash = Convert.ToHexString(SHA256.HashData(fileBytes));

        _storageClientMock
            .Setup(client => client.DownloadSourceCvAsync(cv.FileUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));
        _storageClientMock
            .Setup(client => client.UploadRetainedRawAsync(
                $"retained-unlocks/{unlockId}",
                It.IsAny<Stream>(),
                false,
                "authenticated",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadOutcome(true, null));

        var sut = CreateSut();

        var first = await sut.CaptureAsync(unlockId, cv, CancellationToken.None);
        var second = await sut.CaptureAsync(unlockId, cv, CancellationToken.None);

        first.StorageKey.Should().Be($"retained-unlocks/{unlockId}");
        second.StorageKey.Should().Be(first.StorageKey);
        first.FileName.Should().Be("candidate_cv.pdf");
        first.ContentHash.Should().Be(expectedHash);
    }

    [Fact]
    [Trait("Requirement", "R-11")]
    public async Task CaptureAsync_DoesNotOverwriteAnExistingSnapshot()
    {
        var unlockId = Guid.NewGuid();
        var cv = CreateCv("resume.docx", "https://res.cloudinary.com/demo/raw/upload/v1/cv/resume.docx");
        var fileBytes = Encoding.UTF8.GetBytes("mock docx content");

        _storageClientMock
            .Setup(client => client.DownloadSourceCvAsync(cv.FileUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));
        _storageClientMock
            .Setup(client => client.UploadRetainedRawAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                false,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadOutcome(true, null));

        var sut = CreateSut();

        await sut.CaptureAsync(unlockId, cv, CancellationToken.None);

        _storageClientMock.Verify(
            client => client.UploadRetainedRawAsync(
                $"retained-unlocks/{unlockId}",
                It.IsAny<Stream>(),
                false,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    [Trait("Requirement", "R-11")]
    public async Task CaptureAsync_UsesPrivateAuthenticatedDeliveryAndOverwriteFalse()
    {
        var unlockId = Guid.NewGuid();
        var cv = CreateCv("resume.pdf", "https://res.cloudinary.com/demo/raw/upload/v1/cv/resume.pdf");
        var fileBytes = Encoding.UTF8.GetBytes("mock pdf content");

        _storageClientMock
            .Setup(client => client.DownloadSourceCvAsync(cv.FileUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));
        _storageClientMock
            .Setup(client => client.UploadRetainedRawAsync(
                $"retained-unlocks/{unlockId}",
                It.IsAny<Stream>(),
                false,
                "authenticated",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadOutcome(true, null));

        var sut = CreateSut();

        await sut.CaptureAsync(unlockId, cv, CancellationToken.None);

        _storageClientMock.Verify(
            client => client.UploadRetainedRawAsync(
                $"retained-unlocks/{unlockId}",
                It.IsAny<Stream>(),
                false,
                "authenticated",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-11")]
    public async Task CaptureAsync_Failure_DoesNotReturnAUsableSnapshot()
    {
        var unlockId = Guid.NewGuid();
        var cv = CreateCv("broken.pdf", "https://res.cloudinary.com/demo/raw/upload/v1/cv/broken.pdf");

        _storageClientMock
            .Setup(client => client.DownloadSourceCvAsync(cv.FileUrl!, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Download failed"));

        var sut = CreateSut();

        var action = () => sut.CaptureAsync(unlockId, cv, CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*RETAINED_CV_CAPTURE_FAILED*");
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task CreateAuthorizedReadUrlAsync_UnknownKey_Rejects()
    {
        var sut = CreateSut();

        var actionNull = () => sut.CreateAuthorizedReadUrlAsync(null!, CancellationToken.None);
        var actionEmpty = () => sut.CreateAuthorizedReadUrlAsync(string.Empty, CancellationToken.None);

        await actionNull.Should().ThrowAsync<ArgumentException>();
        await actionEmpty.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task CreateAuthorizedReadUrlAsync_UsesBoundedExpiry()
    {
        const string storageKey = "retained-unlocks/00000000-0000-0000-0000-000000000001";
        const string signedUrl = "https://res.cloudinary.com/demo/raw/authenticated/s--signed--/retained-unlocks/00000000-0000-0000-0000-000000000001?expires_at=1700000000";

        _storageClientMock
            .Setup(client => client.GenerateSignedDownloadUrl(storageKey, It.Is<TimeSpan>(ts => ts > TimeSpan.Zero && ts <= TimeSpan.FromHours(2))))
            .Returns(signedUrl);

        var sut = CreateSut();

        var result = await sut.CreateAuthorizedReadUrlAsync(storageKey, CancellationToken.None);

        result.Should().Be(signedUrl);
        _storageClientMock.Verify(
            client => client.GenerateSignedDownloadUrl(storageKey, It.Is<TimeSpan>(ts => ts > TimeSpan.Zero && ts <= TimeSpan.FromHours(2))),
            Times.Once);
    }

    [Fact]
    [Trait("Requirement", "R-10")]
    public async Task SnapshotStore_LogsContainNoSourceOrRetainedUrl()
    {
        var unlockId = Guid.NewGuid();
        const string secretSourceUrl = "https://res.cloudinary.com/demo/raw/upload/v1/cv/secret-candidate-name.pdf";
        const string secretSignedUrl = "https://res.cloudinary.com/demo/raw/authenticated/s--SECRETTOKEN--/retained.pdf";
        var cv = CreateCv("secret-candidate-name.pdf", secretSourceUrl);
        var fileBytes = Encoding.UTF8.GetBytes("John Doe secret resume content");

        _storageClientMock
            .Setup(client => client.DownloadSourceCvAsync(cv.FileUrl!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));
        _storageClientMock
            .Setup(client => client.UploadRetainedRawAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                false,
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CloudinaryUploadOutcome(true, null));
        _storageClientMock
            .Setup(client => client.GenerateSignedDownloadUrl(It.IsAny<string>(), It.IsAny<TimeSpan>()))
            .Returns(secretSignedUrl);

        var sut = CreateSut();

        var snapshot = await sut.CaptureAsync(unlockId, cv, CancellationToken.None);
        await sut.CreateAuthorizedReadUrlAsync(snapshot.StorageKey, CancellationToken.None);

        foreach (var log in _logEntries)
        {
            log.Should().NotContain(secretSourceUrl);
            log.Should().NotContain(secretSignedUrl);
            log.Should().NotContain("John Doe");
            log.Should().NotContain("SECRETTOKEN");
        }
    }

    private CloudinaryRecruiterUnlockedCvSnapshotStore CreateSut()
    {
        return new CloudinaryRecruiterUnlockedCvSnapshotStore(
            _storageClientMock.Object,
            _loggerMock.Object);
    }

    private static Cvs CreateCv(string fileName, string fileUrl) => new()
    {
        Id = Guid.NewGuid(),
        UserId = Guid.NewGuid(),
        FileName = fileName,
        FileUrl = fileUrl,
        ParseStatus = "SUCCESS"
    };
}
