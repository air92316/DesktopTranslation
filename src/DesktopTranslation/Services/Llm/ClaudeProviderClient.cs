using DesktopTranslation.Models;

namespace DesktopTranslation.Services.Llm;

internal sealed class ClaudeProviderClient : IProviderClient
{
    private readonly string _apiKey;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly double _temperature;

    public ClaudeProviderClient(string apiKey, string model, int maxTokens, double temperature)
    {
        _apiKey = apiKey;
        _model = model;
        _maxTokens = maxTokens;
        _temperature = temperature;
    }

    public async Task<string> CompleteAsync(string systemPrompt, string userText, CancellationToken ct)
    {
        var client = new Claudia.Anthropic { ApiKey = _apiKey };
        var response = await client.Messages.CreateAsync(new()
        {
            Model = _model,
            MaxTokens = _maxTokens,
            Temperature = _temperature,
            System = systemPrompt,
            Messages = [new() { Role = "user", Content = userText }],
        }, cancellationToken: ct);

        return response.Content[0].Text ?? "";
    }

    public ErrorKind ClassifyError(Exception ex, CancellationToken ct)
        => ProviderErrorHelpers.Classify(ex, ct);
}
