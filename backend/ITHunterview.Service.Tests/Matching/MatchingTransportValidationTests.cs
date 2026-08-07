using System.Net;
using System.Text;
using FluentAssertions;
using ITHunterview.Service.Service.Matching;

namespace ITHunterview.Service.Tests.Matching;

public sealed class MatchingTransportValidationTests
{
    [Fact]
    public async Task BoundedReader_RejectsContentLengthAboveLimit()
    {
        using var content = new StringContent("small", Encoding.UTF8, "application/json");
        content.Headers.ContentLength = 10;

        var action = () => BoundedHttpContentReader.ReadAsStringAsync(content, 5);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI_RESPONSE_TOO_LARGE");
    }

    [Fact]
    public async Task BoundedReader_RejectsChunkedBodyWhenItExceedsLimit()
    {
        using var content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("123456789")));

        var action = () => BoundedHttpContentReader.ReadAsStringAsync(content, 5);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AI_RESPONSE_TOO_LARGE");
    }

    [Fact]
    public async Task BoundedReader_ReturnsValidUtf8WithinLimit()
    {
        using var content = new StringContent("{\"ok\":true}", Encoding.UTF8, "application/json");

        var result = await BoundedHttpContentReader.ReadAsStringAsync(content, 100);

        result.Should().Be("{\"ok\":true}");
    }
}
