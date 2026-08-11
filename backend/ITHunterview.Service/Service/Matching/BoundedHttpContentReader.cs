using System.Buffers;
using System.Net.Http;
using System.Text;

namespace ITHunterview.Service.Service.Matching;

/// <summary>
/// Reads provider responses without allowing an unexpectedly large body to be
/// buffered in memory or written to logs.
/// </summary>
public static class BoundedHttpContentReader
{
    public const int DefaultMaxBytes = 1_048_576;

    public static async Task<string> ReadAsStringAsync(
        HttpContent content,
        int maxBytes = DefaultMaxBytes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (maxBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxBytes));

        if (content.Headers.ContentLength is > 0 and var contentLength && contentLength > maxBytes)
            throw new InvalidOperationException("AI_RESPONSE_TOO_LARGE");

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = ArrayPool<byte>.Shared.Rent(Math.Min(81920, maxBytes));
        try
        {
            var initialCapacity = content.Headers.ContentLength is long length && length <= maxBytes
                ? (int)length
                : Math.Min(4096, maxBytes);
            await using var output = new MemoryStream(initialCapacity);
            var totalBytes = 0;
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (read == 0)
                    break;

                totalBytes += read;
                if (totalBytes > maxBytes)
                    throw new InvalidOperationException("AI_RESPONSE_TOO_LARGE");
                await output.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
            }

            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true)
                .GetString(output.GetBuffer(), 0, checked((int)output.Length));
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
