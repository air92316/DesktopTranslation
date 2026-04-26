using DesktopTranslation.Services.Llm;

namespace DesktopTranslation.Tests.Services;

public class LlmModelCatalogTests
{
    [Fact]
    public void GetModels_Claude_ReturnsNonEmpty()
    {
        var models = LlmModelCatalog.GetModels("claude");
        Assert.True(models.Count >= 4, "Claude catalog should include curated entries plus custom");
    }

    [Fact]
    public void GetModels_OpenAi_ReturnsNonEmpty()
    {
        var models = LlmModelCatalog.GetModels("openai");
        Assert.True(models.Count >= 4, "OpenAI catalog should include curated entries plus custom");
    }

    [Fact]
    public void GetModels_Gemini_ReturnsCuratedList()
    {
        var models = LlmModelCatalog.GetModels("gemini");
        Assert.True(models.Count >= 3, "Gemini catalog should include curated entries plus custom");
        Assert.Contains(models, m => m.IsCustom);
    }

    [Fact]
    public void GetModels_Unknown_ReturnsEmpty()
    {
        var models = LlmModelCatalog.GetModels("unknown");
        Assert.Empty(models);
    }

    [Fact]
    public void GetDefault_Claude_ReturnsHaiku45()
    {
        Assert.Equal("claude-haiku-4-5-20251001", LlmModelCatalog.GetDefault("claude"));
    }

    [Fact]
    public void GetDefault_OpenAi_ReturnsGpt54Nano()
    {
        Assert.Equal("gpt-5.4-nano", LlmModelCatalog.GetDefault("openai"));
    }

    [Fact]
    public void GetDefault_Gemini_ReturnsFlash25()
    {
        Assert.Equal("gemini-2.5-flash", LlmModelCatalog.GetDefault("gemini"));
    }

    [Fact]
    public void Each_Provider_HasCustomEntry()
    {
        Assert.Contains(LlmModelCatalog.GetModels("claude"), m => m.IsCustom);
        Assert.Contains(LlmModelCatalog.GetModels("openai"), m => m.IsCustom);
    }

    [Fact]
    public void Claude_Catalog_ContainsOpus47()
    {
        var models = LlmModelCatalog.GetModels("claude");
        Assert.Contains(models, m => m.Id == "claude-opus-4-7");
    }

    [Fact]
    public void Claude_Catalog_ContainsExpectedAliases()
    {
        var models = LlmModelCatalog.GetModels("claude");
        var ids = models.Select(m => m.Id).ToList();
        Assert.Contains("claude-haiku-4-5-20251001", ids);
        Assert.Contains("claude-sonnet-4-6", ids);
        Assert.Contains("claude-sonnet-4-5-20250929", ids);
        Assert.Contains("claude-opus-4-5-20251101", ids);
        Assert.Contains("claude-opus-4-7", ids);
    }

    [Fact]
    public void OpenAi_Catalog_ContainsExpectedAliases()
    {
        var models = LlmModelCatalog.GetModels("openai");
        var ids = models.Select(m => m.Id).ToList();
        Assert.Contains("gpt-5.4-nano", ids);
        Assert.Contains("gpt-5.4-mini", ids);
        Assert.Contains("gpt-5.4", ids);
    }

    [Fact]
    public void OpenAi_Default_ShouldNotBeRetiredModel()
    {
        // gpt-4o-mini sunset on 2026-02-27.
        // Source: https://openai.com/index/retiring-gpt-4o-and-older-models/
        Assert.NotEqual("gpt-4o-mini", LlmModelCatalog.GetDefault("openai"));
        Assert.NotEqual("gpt-4o", LlmModelCatalog.GetDefault("openai"));
    }

    [Fact]
    public void OpenAi_Catalog_ShouldNotIncludeRetiredModels()
    {
        // gpt-4o-mini sunset 2026-02-27. Avoid offering it as a selectable option.
        var ids = LlmModelCatalog.GetModels("openai").Select(m => m.Id).ToList();
        Assert.DoesNotContain("gpt-4o-mini", ids);
        Assert.DoesNotContain("gpt-4o", ids);
        Assert.DoesNotContain("gpt-5-nano", ids);
    }

    [Fact]
    public void Gemini_Catalog_ContainsExpectedAliases()
    {
        var models = LlmModelCatalog.GetModels("gemini");
        var ids = models.Select(m => m.Id).ToList();
        Assert.Contains("gemini-2.5-flash", ids);
        Assert.Contains("gemini-2.5-flash-lite", ids);
    }

    [Fact]
    public void Gemini_Catalog_ShouldNotIncludeRetiredModels()
    {
        var ids = LlmModelCatalog.GetModels("gemini").Select(m => m.Id).ToList();
        Assert.DoesNotContain("gemini-2.0-flash", ids);
        Assert.DoesNotContain(ids, id => id.Contains("preview", StringComparison.OrdinalIgnoreCase));
    }
}
