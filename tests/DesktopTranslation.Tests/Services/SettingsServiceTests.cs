using System.Text.Json;
using DesktopTranslation.Models;
using DesktopTranslation.Services;

namespace DesktopTranslation.Tests.Services;

public class SettingsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly SettingsService _service;

    public SettingsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"dt_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _service = new SettingsService(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    [Fact]
    public void Load_ReturnsDefaults_WhenFileDoesNotExist()
    {
        var settings = _service.Load();
        Assert.Equal(720, settings.WindowWidth);
        Assert.Equal("google", settings.Engine);
    }

    [Fact]
    public void Save_Then_Load_RoundTrips()
    {
        var settings = new AppSettings { Engine = "llm", ApiKey = "test-key" };
        _service.Save(settings);

        var loaded = _service.Load();
        Assert.Equal("llm", loaded.Engine);
        Assert.Equal("test-key", loaded.ApiKey);
    }

    [Fact]
    public void Load_HandlesCorruptFile_ReturnsDefaults()
    {
        File.WriteAllText(Path.Combine(_tempDir, "settings.json"), "not json");
        var settings = _service.Load();
        Assert.Equal(720, settings.WindowWidth);
    }
}
