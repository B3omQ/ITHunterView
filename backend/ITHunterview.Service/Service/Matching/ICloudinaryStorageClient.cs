using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ITHunterview.Service.Service.Matching;

public sealed record CloudinaryUploadOutcome(bool Success, string? ErrorMessage);

public interface ICloudinaryStorageClient
{
    Task<Stream> DownloadSourceCvAsync(string sourceUrl, CancellationToken ct);
    Task<CloudinaryUploadOutcome> UploadRetainedRawAsync(
        string storageKey,
        Stream fileStream,
        bool overwrite,
        string deliveryType,
        CancellationToken ct);
    string GenerateSignedDownloadUrl(string storageKey, TimeSpan expiry);
}
