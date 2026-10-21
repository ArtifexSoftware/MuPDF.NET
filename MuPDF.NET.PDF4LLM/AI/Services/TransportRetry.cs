using System.Net;
using System.Net.Http;
using System.Net.Sockets;

namespace MuPDF.NET.PDF4LLM.AI.Services;

/// <summary>
/// Retries a call when the transport drops or the server returns a transient 5xx.
/// </summary>
internal static class TransportRetry
{
    public const int Attempts = 3;

    public static async Task<T> RunAsync<T>(
        Func<Task<T>> action,
        CancellationToken cancellationToken = default)
    {
        Exception? last = null;
        for (int attempt = 1; attempt <= Attempts; attempt++)
        {
            try
            {
                return await action().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsReset(ex) && attempt < Attempts)
            {
                last = ex;
                await Task.Delay(TimeSpan.FromSeconds(attempt * 2), cancellationToken).ConfigureAwait(false);
            }
        }

        throw last!;
    }

    public static bool IsReset(Exception ex)
    {
        for (Exception? current = ex; current != null; current = current.InnerException)
        {
            if (current is HttpRequestException http)
            {
                if (http.StatusCode is HttpStatusCode status)
                    return IsTransientStatus(status);
                return true;
            }

            if (current is IOException or SocketException)
                return true;
        }

        return false;
    }

    static bool IsTransientStatus(HttpStatusCode status)
    {
        int code = (int)status;
        return code is 500 or 502 or 503 or 504;
    }
}
