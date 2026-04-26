using System.ClientModel;
using DesktopTranslation.Models;
using OpenAI;
using OpenAI.Chat;

namespace DesktopTranslation.Services.Llm;

internal sealed class OpenAiProviderClient : IProviderClient
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _baseUrl;
    private readonly double _temperature;
    private readonly int _maxTokens;

    public OpenAiProviderClient(string apiKey, string model, string baseUrl, double temperature, int maxTokens)
    {
        _apiKey = apiKey;
        _model = model;
        _baseUrl = baseUrl;
        _temperature = temperature;
        _maxTokens = maxTokens;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userText, CancellationToken ct)
    {
        var credential = new ApiKeyCredential(_apiKey);
        var client = string.IsNullOrWhiteSpace(_baseUrl)
            ? new OpenAIClient(credential)
            : new OpenAIClient(credential, new OpenAIClientOptions { Endpoint = new Uri(_baseUrl) });

        var chatClient = client.GetChatClient(_model);
        var options = new ChatCompletionOptions
        {
            Temperature = (float)_temperature,
            MaxOutputTokenCount = _maxTokens,
        };

        var response = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userText),
            ],
            options,
            ct);

        return response.Value.Content[0].Text ?? "";
    }

    public ErrorKind ClassifyError(Exception ex, CancellationToken ct)
        => ProviderErrorHelpers.Classify(ex, ct);
}
