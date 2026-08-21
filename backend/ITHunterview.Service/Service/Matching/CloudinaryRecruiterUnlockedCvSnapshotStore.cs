using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ITHunterview.Domain.Entities;
using ITHunterview.Service.DTOs.Cv.Matching;
using ITHunterview.Service.Interface.Service.Matching;
using Microsoft.Extensions.Logging;

namespace ITHunterview.Service.Service.Matching;

public sealed class CloudinaryRecruiterUnlockedCvSnapshotStore : IRecruiterUnlockedCvSnapshotStore
{
    private readonly ICloudinaryStorageClient _storageClient;
    private readonly ILogger<CloudinaryRecruiterUnlockedCvSnapshotStore> _logger;

    public CloudinaryRecruiterUnlockedCvSnapshotStore(
        ICloudinaryStorageClient storageClient,
        ILogger<CloudinaryRecruiterUnlockedCvSnapshotStore> logger)
    {
        _storageClient = storageClient ?? throw new ArgumentNullException(nameof(storageClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RetainedCvSnapshot> CaptureAsync(Guid unlockId, Cvs cv, CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(cv);
        if (string.IsNullOrWhiteSpace(cv.FileUrl))
        {
            throw new InvalidOperationException("RETAINED_CV_CAPTURE_FAILED: CV file URL is missing.");
        }

        var storageKey = $"retained-unlocks/{unlockId}";
        try
        {
            using var stream = await _storageClient.DownloadSourceCvAsync(cv.FileUrl, ct);
            ct.ThrowIfCancellationRequested();

            var fileBytes = stream is MemoryStream ms ? ms.ToArray() : ReadFully(stream);
            var contentHash = Convert.ToHexString(SHA256.HashData(fileBytes));

            using var uploadStream = new MemoryStream(fileBytes);
            var outcome = await _storageClient.UploadRetainedRawAsync(
                storageKey,
                uploadStream,
                overwrite: false,
                deliveryType: "authenticated",
                ct);

            if (!outcome.Success)
            {
                throw new InvalidOperationException($"RETAINED_CV_CAPTURE_FAILED: {outcome.ErrorMessage}");
            }

            _logger.LogInformation("Retained CV captured successfully for unlock {UnlockId}", unlockId);

            return new RetainedCvSnapshot(
                StorageKey: storageKey,
                FileName: cv.FileName ?? "cv.pdf",
                ContentHash: contentHash,
                CreatedAt: DateTime.UtcNow);
        }
        catch (Exception ex) when (ex is not InvalidOperationException)
        {
            _logger.LogError(ex, "Failed to capture retained CV for unlock {UnlockId}", unlockId);
            throw new InvalidOperationException("RETAINED_CV_CAPTURE_FAILED: Could not download or store retained copy.", ex);
        }
    }

    public Task<string> CreateAuthorizedReadUrlAsync(string storageKey, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key cannot be null or empty.", nameof(storageKey));
        }

        var signedUrl = _storageClient.GenerateSignedDownloadUrl(storageKey, TimeSpan.FromHours(1));
        _logger.LogInformation("Generated authorized read URL for storage key {StorageKey}", storageKey);
        return Task.FromResult(signedUrl);
    }

    private static byte[] ReadFully(Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }
}
