using System.ClientModel;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using DesktopTranslation.Models;

namespace DesktopTranslation.Services.Llm;

internal static class ProviderErrorHelpers
{
    public static ErrorKind Classify(Exception exception, CancellationToken ct)
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

    public static bool ContainsSocketException(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is SocketException)
                return true;
        }

        return false;
    }

    public static bool HasStatusCode(Exception exception, HttpStatusCode expectedStatusCode)
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

        if (exception is ClientResultException clientResultException)
            return clientResultException.Status;

        var property = exception.GetType().GetProperty("Status")
            ?? exception.GetType().GetProperty("StatusCode");

        if (property?.GetValue(exception) is HttpStatusCode enumStatusCode)
            return (int)enumStatusCode;

        if (property?.GetValue(exception) is int intStatusCode)
            return intStatusCode;

        return null;
    }
}
