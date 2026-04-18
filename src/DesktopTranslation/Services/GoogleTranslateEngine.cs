using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using GTranslate.Translators;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services;

public class GoogleTranslateEngine : ITranslationEngine
{
    private readonly GoogleTranslator _translator = new();

    public string Name => "Google";

    public async Task<TranslationResult> TranslateAsync(
        string text, string targetLanguage, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new TranslationResult("", "unknown", false, "Input text is empty");

        try
        {
            var result = await _translator.TranslateAsync(text, targetLanguage);
            return new TranslationResult(
                TranslatedText: result.Translation,
                DetectedSourceLanguage: result.SourceLanguage.ISO6391 ?? "unknown",
                IsSuccess: true);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            Debug.WriteLine($"Google translation error: {ex}");
            var errorKind = ClassifyError(ex, ct);
            return new TranslationResult(
                TranslatedText: "",
                DetectedSourceLanguage: "unknown",
                IsSuccess: false,
                ErrorMessage: "Translation failed. Please check your connection and try again.",
                ErrorKind: errorKind);
        }
    }

    private static ErrorKind ClassifyError(Exception exception, CancellationToken ct)
    {
        if (HasStatusCode(exception, HttpStatusCode.Unauthorized))
            return ErrorKind.ApiKey;

        if (HasStatusCode(exception, (HttpStatusCode)429))
            return ErrorKind.RateLimit;

        if (exception is TaskCanceledException && !ct.IsCancellationRequested)
            return ErrorKind.Timeout;

        if (exception is HttpRequestException || ContainsSocketException(exception))
            return ErrorKind.Network;

        return ErrorKind.Unknown;
    }

    private static bool ContainsSocketException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException)
                return true;
        }

        return false;
    }

    private static bool HasStatusCode(Exception exception, HttpStatusCode expectedStatusCode)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            var statusCode = GetStatusCodeValue(current);
            if (statusCode == (int)expectedStatusCode)
                return true;
        }

        return false;
    }

    private static int? GetStatusCodeValue(Exception exception)
    {
        if (exception is HttpRequestException httpException && httpException.StatusCode is { } httpStatusCode)
            return (int)httpStatusCode;

        var property = exception.GetType().GetProperty("Status")
            ?? exception.GetType().GetProperty("StatusCode");

        if (property?.GetValue(exception) is HttpStatusCode enumStatusCode)
            return (int)enumStatusCode;

        if (property?.GetValue(exception) is int intStatusCode)
            return intStatusCode;

        return null;
    }
}
