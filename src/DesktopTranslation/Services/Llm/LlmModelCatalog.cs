namespace DesktopTranslation.Services.Llm;

public sealed record LlmModelEntry(string Id, string DisplayName, bool IsCustom = false);

public static class LlmModelCatalog
{
    public static IReadOnlyList<LlmModelEntry> GetModels(string provider) => provider switch
    {
        "claude" => ClaudeModels,
        "openai" => OpenAiModels,
        "gemini" => GeminiModels,
        _ => Array.Empty<LlmModelEntry>(),
    };

    public static string GetDefault(string provider) => provider switch
    {
        "claude" => "claude-haiku-4-5-20251001",
        "openai" => "gpt-5.4-nano",
        "gemini" => "",
        _ => "",
    };

    private static readonly IReadOnlyList<LlmModelEntry> ClaudeModels = new[]
    {
        new LlmModelEntry("claude-haiku-4-5-20251001", "Haiku 4.5（預設、CP 最高）"),
        new LlmModelEntry("claude-sonnet-4-6", "Sonnet 4.6（品質與速度平衡）"),
        new LlmModelEntry("claude-opus-4-7", "Opus 4.7（最強、文學級翻譯）"),
        new LlmModelEntry("claude-sonnet-4-5-20250929", "Sonnet 4.5"),
        new LlmModelEntry("claude-opus-4-5-20251101", "Opus 4.5"),
        new LlmModelEntry("", "自訂…", IsCustom: true),
    };

    private static readonly IReadOnlyList<LlmModelEntry> OpenAiModels = new[]
    {
        new LlmModelEntry("gpt-5.4-nano", "gpt-5.4-nano（預設、最便宜最快）"),
        new LlmModelEntry("gpt-5.4-mini", "gpt-5.4-mini（品質升級）"),
        new LlmModelEntry("gpt-5.4", "gpt-5.4（標準）"),
        new LlmModelEntry("", "自訂…", IsCustom: true),
    };

    private static readonly IReadOnlyList<LlmModelEntry> GeminiModels = Array.Empty<LlmModelEntry>();
}
