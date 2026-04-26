using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services.Llm;

internal sealed class GeminiProviderClient : IProviderClient
{
    private static readonly HttpClient SharedHttpClient = new();
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _apiKey;
    private readonly string _model;
    private readonly double _temperature;
    private readonly int _maxTokens;
    private readonly HttpClient _httpClient;

    public GeminiProviderClient(string apiKey, string model, double temperature, int maxTokens)
        : this(apiKey, model, temperature, maxTokens, SharedHttpClient)
    {
    }

    internal GeminiProviderClient(
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        HttpMessageHandler handler)
        : this(apiKey, model, temperature, maxTokens, new HttpClient(handler))
    {
    }

    private GeminiProviderClient(
        string apiKey,
        string model,
        double temperature,
        int maxTokens,
        HttpClient httpClient)
    {
        _apiKey = apiKey;
        _model = model;
        _temperature = temperature;
        _maxTokens = maxTokens;
        _httpClient = httpClient;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userText, CancellationToken ct)
    {
        var model = Uri.EscapeDataString(_model);
        var apiKey = Uri.EscapeDataString(_apiKey);
        var endpoint =
            $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

        var body = JsonSerializer.Serialize(new
        {
            SystemInstruction = new
            {
                Parts = new[]
                {
                    new { Text = systemPrompt },
                },
            },
            Contents = new[]
            {
                new
                {
                    Role = "user",
                    Parts = new[]
                    {
                        new { Text = userText },
                    },
                },
            },
            GenerationConfig = new
            {
                Temperature = _temperature,
                MaxOutputTokens = _maxTokens,
            },
        }, JsonOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

        using var response = await _httpClient.SendAsync(request, ct);
        var responseBody = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            var message = string.IsNullOrWhiteSpace(responseBody)
                ? $"Gemini request failed with status code {(int)response.StatusCode}"
                : responseBody;
            throw new HttpRequestException(message, null, response.StatusCode);
        }

        using var document = JsonDocument.Parse(responseBody);
        if (TryReadText(document.RootElement, out var text))
            return text;

        throw new InvalidOperationException("Gemini response missing content");
    }

    public ErrorKind ClassifyError(Exception ex, CancellationToken ct)
    {
        if (ProviderErrorHelpers.HasStatusCode(ex, HttpStatusCode.Forbidden))
            return ErrorKind.ApiKey;

        return ProviderErrorHelpers.Classify(ex, ct);
    }

    private static bool TryReadText(JsonElement root, out string text)
    {
        text = string.Empty;

        if (!root.TryGetProperty("candidates", out var candidates) ||
            candidates.ValueKind != JsonValueKind.Array ||
            candidates.GetArrayLength() == 0)
        {
            return false;
        }

        var candidate = candidates[0];
        if (!candidate.TryGetProperty("content", out var content) ||
            !content.TryGetProperty("parts", out var parts) ||
            parts.ValueKind != JsonValueKind.Array ||
            parts.GetArrayLength() == 0)
        {
            return false;
        }

        var firstPart = parts[0];
        if (!firstPart.TryGetProperty("text", out var textElement) ||
            textElement.ValueKind != JsonValueKind.String)
        {
            return false;
        }

        var parsed = textElement.GetString();
        if (parsed is null)
            return false;

        text = parsed;
        return true;
    }
}
