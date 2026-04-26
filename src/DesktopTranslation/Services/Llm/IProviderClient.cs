using DesktopTranslation.Models;

namespace DesktopTranslation.Services.Llm;

internal interface IProviderClient
{
    Task<string> CompleteAsync(string systemPrompt, string userText, CancellationToken ct);
    ErrorKind ClassifyError(Exception ex, CancellationToken ct);
}
