using System.Net;
using System.Text.Json;
using ModelContextProtocol;

namespace FinGround.McpServer.Api;

/// <summary>
/// Mirrors the bodies written by API/Middleware/GlobalExceptionMiddleware.cs
/// (application/problem+json, camelCase, plus lockedUntil on 423) and by the rate
/// limiter's OnRejected handler in API/Program.cs (adds retryAfterSeconds on 429).
/// </summary>
internal sealed record ApiProblem(
    string? Type,
    string? Title,
    int? Status,
    string? Detail,
    string? TraceId,
    DateTimeOffset? LockedUntil,
    int? RetryAfterSeconds);

internal static class ApiProblemExtensions
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Turns a non-2xx API response into an McpException whose message is useful to a model.
    /// Only McpException messages reach the client — any other exception type is replaced with a
    /// generic string by the SDK to avoid leaking internals, which would make failures undebuggable.
    /// </summary>
    public static async Task ThrowIfUnsuccessfulAsync(
        this HttpResponseMessage response, bool isStdio, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
            return;

        var problem = await TryReadProblemAsync(response, ct);
        var detail = string.IsNullOrWhiteSpace(problem?.Detail) ? null : problem!.Detail;

        var message = response.StatusCode switch
        {
            HttpStatusCode.Unauthorized when isStdio =>
                "Not authenticated against the FinGround API. Call the 'login' tool with your "
                + "email and password first, or start the server with FinGroundApi__BearerToken set.",

            HttpStatusCode.Unauthorized =>
                "The Authorization header on the MCP request was missing, expired, or invalid. "
                + "Obtain a token from POST /api/auth/login and send it as 'Authorization: Bearer <token>'.",

            HttpStatusCode.Forbidden =>
                $"Forbidden{Suffix(detail)} This operation requires a role the authenticated user does not have.",

            HttpStatusCode.NotFound =>
                $"Not found{Suffix(detail)}",

            HttpStatusCode.BadRequest =>
                $"Invalid request{Suffix(detail)}",

            HttpStatusCode.UnprocessableEntity =>
                $"Business rule violation{Suffix(detail)}",

            // 423 Locked — the API's AccountLockedException path.
            (HttpStatusCode)423 =>
                $"Account locked{(problem?.LockedUntil is { } until ? $" until {until:u}" : "")}{Suffix(detail)}",

            HttpStatusCode.TooManyRequests =>
                "Rate limited by the FinGround API"
                + (problem?.RetryAfterSeconds is { } retry ? $"; retry after {retry} seconds." : ". Retry shortly.")
                + " The login and register endpoints are throttled per client IP.",

            _ =>
                $"FinGround API error {(int)response.StatusCode} ({problem?.Title ?? response.ReasonPhrase})"
                + Suffix(detail)
                + (problem?.TraceId is { Length: > 0 } trace ? $" [traceId={trace}]" : "")
        };

        throw new McpException(message);

        static string Suffix(string? detail) => detail is null ? "." : $": {detail}";
    }

    /// <summary>
    /// A connection failure otherwise surfaces to the model as a scrubbed generic error,
    /// leaving it no way to tell "API is down" from "the tool is broken".
    /// </summary>
    public static McpException ToUnreachableException(string baseUrl, Exception inner) =>
        new($"Could not reach the FinGround API at {baseUrl}. Is it running? "
            + "Start it with: dotnet run --project API --no-launch-profile "
            + $"({inner.GetType().Name}: {inner.Message})", inner);

    private static async Task<ApiProblem?> TryReadProblemAsync(
        HttpResponseMessage response, CancellationToken ct)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            return string.IsNullOrWhiteSpace(body)
                ? null
                : JsonSerializer.Deserialize<ApiProblem>(body, JsonOptions);
        }
        catch (Exception ex) when (ex is JsonException or HttpRequestException or TaskCanceledException)
        {
            // Not every error body is problem+json (a proxy 502, for example). Fall back to the status code.
            return null;
        }
    }
}
