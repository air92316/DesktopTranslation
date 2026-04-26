using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using DesktopTranslation.Models;
using DesktopTranslation.Services.Llm;

namespace DesktopTranslation.Tests.Services;

public class IProviderClientTests
{
    [Fact]
    public void ClaudeProviderClient_Constructs_WithValidArgs()
    {
        var client = new ClaudeProviderClient("api-key", "claude-haiku-4-5-20251001", 2048, 0.3);
        Assert.NotNull(client);
    }

    [Fact]
    public void OpenAiProviderClient_Constructs_WithEmptyBaseUrl()
    {
        var client = new OpenAiProviderClient("api-key", "gpt-4o-mini", "", 0.3, 2048);
        Assert.NotNull(client);
    }

    [Fact]
    public void OpenAiProviderClient_Constructs_WithCustomBaseUrl()
    {
        var client = new OpenAiProviderClient(
            "api-key", "gpt-4o-mini", "https://api.openai.com/v1", 0.3, 2048);
        Assert.NotNull(client);
    }

    [Fact]
    public void LlmTranslateEngine_Constructs_WithValidArgs()
    {
        var engine = new LlmTranslateEngine("claude", "key", "", "", 0.3, 2048);
        Assert.Equal("LLM (claude)", engine.Name);
    }

    [Fact]
    public void LlmTranslateEngine_EffectiveModel_FallsBackToDefault_WhenModelEmpty()
    {
        var engine = new LlmTranslateEngine("claude", "key", "", "", 0.3, 2048);
        Assert.Equal("claude-haiku-4-5-20251001", engine.EffectiveModel);
    }

    [Fact]
    public void LlmTranslateEngine_EffectiveModel_UsesExplicitModel_WhenProvided()
    {
        var engine = new LlmTranslateEngine(
            "openai", "key", "gpt-5.4-nano", "", 0.3, 2048);
        Assert.Equal("gpt-5.4-nano", engine.EffectiveModel);
    }

    [Fact]
    public void LlmTranslateEngine_EffectiveModel_OpenAiDefault_WhenModelEmpty()
    {
        var engine = new LlmTranslateEngine("openai", "key", "", "", 0.3, 2048);
        Assert.Equal("gpt-5.4-nano", engine.EffectiveModel);
    }

    [Fact]
    public void LlmTranslateEngine_EffectiveModel_GeminiDefault_WhenModelEmpty()
    {
        var engine = new LlmTranslateEngine("gemini", "key", "", "", 0.3, 2048);
        Assert.Equal("gemini-2.5-flash", engine.EffectiveModel);
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_Unauthorized_ReturnsApiKey()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var ex = new HttpRequestException("unauthorized", null, HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorKind.ApiKey, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_TooManyRequests_ReturnsRateLimit()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var ex = new HttpRequestException("rate limited", null, (HttpStatusCode)429);
        Assert.Equal(ErrorKind.RateLimit, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_TaskCanceledNoCancel_ReturnsTimeout()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var ex = new TaskCanceledException("timed out");
        Assert.Equal(ErrorKind.Timeout, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_TaskCanceledWithCancel_ReturnsUnknown()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var ex = new TaskCanceledException("user cancelled");
        Assert.Equal(ErrorKind.Unknown, client.ClassifyError(ex, cts.Token));
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_HttpRequestException_ReturnsNetwork()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var ex = new HttpRequestException("network down");
        Assert.Equal(ErrorKind.Network, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_SocketException_ReturnsNetwork()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var inner = new SocketException();
        var ex = new InvalidOperationException("wrapping", inner);
        Assert.Equal(ErrorKind.Network, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void ClaudeProviderClient_ClassifyError_GenericException_ReturnsUnknown()
    {
        var client = new ClaudeProviderClient("key", "claude-haiku-4-5-20251001", 2048, 0.3);
        var ex = new InvalidOperationException("something else");
        Assert.Equal(ErrorKind.Unknown, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void OpenAiProviderClient_ClassifyError_TooManyRequests_ReturnsRateLimit()
    {
        var client = new OpenAiProviderClient("key", "gpt-4o-mini", "", 0.3, 2048);
        var ex = new HttpRequestException("rate limited", null, (HttpStatusCode)429);
        Assert.Equal(ErrorKind.RateLimit, client.ClassifyError(ex, CancellationToken.None));
    }

    [Fact]
    public void OpenAiProviderClient_ClassifyError_Unauthorized_ReturnsApiKey()
    {
        var client = new OpenAiProviderClient("key", "gpt-4o-mini", "", 0.3, 2048);
        var ex = new HttpRequestException("auth", null, HttpStatusCode.Unauthorized);
        Assert.Equal(ErrorKind.ApiKey, client.ClassifyError(ex, CancellationToken.None));
    }
}
