using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using ITHunterview.Service.Config;
using Microsoft.Extensions.Options;

namespace ITHunterview.Service.Service.Matching;

public sealed class CloudinaryStorageClient : ICloudinaryStorageClient
{
    private readonly Cloudinary? _cloudinary;
    private readonly HttpClient _httpClient;

    public CloudinaryStorageClient(IOptions<CloudinarySettings> config, HttpClient httpClient)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        if (config?.Value != null && !string.IsNullOrEmpty(config.Value.CloudName) && config.Value.CloudName != "your-cloud-name")
        {
            var acc = new Account(
                config.Value.CloudName,
                config.Value.ApiKey,
                config.Value.ApiSecret
            );
            _cloudinary = new Cloudinary(acc);
        }
    }

    public async Task<Stream> DownloadSourceCvAsync(string sourceUrl, CancellationToken ct)
    {
        var response = await _httpClient.GetAsync(sourceUrl, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();
        var memoryStream = new MemoryStream();
        await response.Content.CopyToAsync(memoryStream, ct);
        memoryStream.Position = 0;
        return memoryStream;
    }

    public async Task<CloudinaryUploadOutcome> UploadRetainedRawAsync(
        string storageKey,
        Stream fileStream,
        bool overwrite,
        string deliveryType,
        CancellationToken ct)
    {
        if (_cloudinary == null)
        {
            return new CloudinaryUploadOutcome(true, null);
        }

        var uploadParams = new RawUploadParams
        {
            File = new FileDescription(storageKey, fileStream),
            PublicId = storageKey,
            Overwrite = overwrite,
            Type = deliveryType
        };

        var result = await _cloudinary.UploadAsync(uploadParams);
        if (result.Error != null)
        {
            return new CloudinaryUploadOutcome(false, result.Error.Message);
        }

        return new CloudinaryUploadOutcome(true, null);
    }

    public string GenerateSignedDownloadUrl(string storageKey, TimeSpan expiry)
    {
        if (_cloudinary == null)
        {
            return $"https://mock-storage.internal/retained-unlocks/{storageKey}?expires={(int)expiry.TotalSeconds}";
        }

        return _cloudinary.Api.Url.ResourceType("raw")
            .Action("download")
            .Type("authenticated")
            .Signed(true)
            .BuildUrl(storageKey);
    }
}
