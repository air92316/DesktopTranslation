using System.ClientModel;
using DesktopTranslation.Models;
using OpenAI;
using OpenAI.Chat;

namespace DesktopTranslation.Services;

public class LlmTranslateEngine : ITranslationEngine
{
    private readonly string _provider;
    private readonly string _apiKey;

    public LlmTranslateEngine(string provider, string apiKey)
    {
        _provider = provider;
        _apiKey = apiKey;
    }

    public string Name => $"LLM ({_provider})";

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        try
        {
            var targetName = targetLanguage == "en" ? "English" : "Traditional Chinese (zh-TW)";
            var systemPrompt = $"You are a translator. Translate the following text to {targetName}. " +
                               "Output ONLY the translation, no explanations.";

            if (_provider == "openai")
            {
                return await TranslateWithOpenAiAsync(systemPrompt, text, ct);
            }
            else
            {
                return await TranslateWithClaudeAsync(systemPrompt, text, ct);
            }
        }
        catch (Exception ex)
        {
            return new TranslationResult("", "unknown", false, ex.Message);
        }
    }

    private async Task<TranslationResult> TranslateWithOpenAiAsync(
        string systemPrompt, string text, CancellationToken ct)
    {
        var client = new OpenAIClient(new ApiKeyCredential(_apiKey));
        var chatClient = client.GetChatClient("gpt-4o-mini");
        var response = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(text)
            ],
            cancellationToken: ct);

        var translated = response.Value.Content[0].Text ?? "";
        return new TranslationResult(translated, "auto", true);
    }

    private async Task<TranslationResult> TranslateWithClaudeAsync(
        string systemPrompt, string text, CancellationToken ct)
    {
        var client = new Claudia.Anthropic { ApiKey = _apiKey };
        var response = await client.Messages.CreateAsync(new()
        {
            Model = "claude-sonnet-4-20250514",
            MaxTokens = 4096,
            System = systemPrompt,
            Messages = [new() { Role = "user", Content = text }]
        });

        var translated = response.Content[0].Text ?? "";
        return new TranslationResult(translated, "auto", true);
    }
}
