using System.Net;
using System.Net.Http;
using DesktopTranslation.Models;
using DesktopTranslation.Services.Llm;

namespace DesktopTranslation.Tests.Services;

public class GeminiProviderClientTests
{
    [Fact]
    public async Task CompleteAsync_Returns_Text_OnSuccess()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"candidates\":[{\"content\":{\"parts\":[{\"text\":\"Hello\"}]}}]}"),
        });
        var client = new GeminiProviderClient("key", "gemini-2.5-flash", 0.3, 2048, handler);

        var result = await client.CompleteAsync("system", "user", CancellationToken.None);

        Assert.Equal("Hello", result);
    }

    [Fact]
    public void ClassifyError_Unauthorized_ReturnsApiKey()
    {
        var client = CreateClient();
        var ex = new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorKind.ApiKey, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClassifyError_Forbidden_ReturnsApiKey()
    {
        var client = CreateClient();
        var ex = new HttpRequestException("forbidden", null, HttpStatusCode.Forbidden);
        Assert.Equal(ErrorKind.ApiKey, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClassifyError_TooManyRequests_ReturnsRateLimit()
    {
        var client = CreateClient();
        var ex = new HttpRequestException("rate limited", null, (HttpStatusCode)429);
        Assert.Equal(ErrorKind.RateLimit, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClassifyError_HttpRequestException_ReturnsNetwork()
    {
        var client = CreateClient();
        var ex = new HttpRequestException("net down");
        Assert.Equal(ErrorKind.Network, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClassifyError_TaskCanceled_NoCancel_ReturnsTimeout()
    {
        var client = CreateClient();
        var ex = new TaskCanceledException("timed out");
        Assert.Equal(ErrorKind.Timeout, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public async Task CompleteAsync_Throws_OnNon2xx()
    {
        using var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError)
        {
            Content = new StringContent("server error"),
        });
        var client = new GeminiProviderClient("key", "gemini-2.5-flash", 0.3, 2048, handler);

        await Assert.ThrowsAsync<HttpRequestException>(
            () => client.CompleteAsync("system", "user", CancellationToken.None));
    }

    private static GeminiProviderClient CreateClient()
    {
        return new GeminiProviderClient(
            "key",
            "gemini-2.5-flash",
            0.3,
            2048,
            new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
        private readonly Exception? _exception;

        public StubHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        public StubHandler(Exception exception)
        {
            _exception = exception;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (_exception is not null)
                throw _exception;

            if (_handler is null)
                throw new InvalidOperationException("Stub handler is not configured");

            return Task.FromResult(_handler(request));
        }
    }
}
