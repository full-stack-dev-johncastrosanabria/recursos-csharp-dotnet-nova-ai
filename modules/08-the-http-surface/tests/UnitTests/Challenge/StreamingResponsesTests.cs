using System.Net;
using Shouldly;
using Training.Module08.Challenge;

namespace Training.Module08.Tests.Challenge;

public sealed class StreamingResponsesTests
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task The_default_reads_the_whole_body_before_it_returns()
    {
        var content = new ProgressContent(chunks: 16, chunkSize: 4096);
        using var client = ClientReturning(content);
        long writtenWhenSendReturned = -1;

        var total = await StreamingResponses.DownloadBufferedAsync(
            client, "report", () => writtenWhenSendReturned = content.BytesWritten, Token);

        total.ShouldBe(content.Total);
        writtenWhenSendReturned.ShouldBe(content.Total);
    }

    [Fact]
    public async Task ResponseHeadersRead_returns_before_any_of_the_body_is_read()
    {
        // The whole point. At this moment the status and headers are known and
        // not one byte of the payload has been touched.
        var content = new ProgressContent(chunks: 16, chunkSize: 4096);
        using var client = ClientReturning(content);
        long writtenWhenSendReturned = -1;

        var total = await StreamingResponses.DownloadStreamedAsync(
            client, "report", () => writtenWhenSendReturned = content.BytesWritten, Token);

        writtenWhenSendReturned.ShouldBe(0);
        total.ShouldBe(content.Total);
    }

    [Fact]
    public async Task Both_forms_agree_on_what_the_body_contained()
    {
        var buffered = new ProgressContent(chunks: 5, chunkSize: 1024);
        var streamed = new ProgressContent(chunks: 5, chunkSize: 1024);
        using var first = ClientReturning(buffered);
        using var second = ClientReturning(streamed);

        var bufferedTotal = await StreamingResponses.DownloadBufferedAsync(first, "report", () => { }, Token);
        var streamedTotal = await StreamingResponses.DownloadStreamedAsync(second, "report", () => { }, Token);

        streamedTotal.ShouldBe(bufferedTotal);
    }

    [Fact]
    public async Task An_empty_body_streams_to_zero_bytes()
    {
        var content = new ProgressContent(chunks: 0, chunkSize: 4096);
        using var client = ClientReturning(content);

        var total = await StreamingResponses.DownloadStreamedAsync(client, "report", () => { }, Token);

        total.ShouldBe(0);
    }

    private static HttpClient ClientReturning(HttpContent content)
        => new(new StubPrimaryHandler((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = content })))
        {
            BaseAddress = new Uri("https://gateway.invalid/"),
        };
}
