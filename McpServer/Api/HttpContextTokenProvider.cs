namespace FinGround.McpServer.Api;

/// <summary>
/// HTTP transport: forwards the bearer token from the incoming MCP request straight
/// through to the FinGround API, so the API's own [Authorize] checks and the
/// InitiatedBy audit identity keep working unchanged.
/// </summary>
/// <remarks>
/// Registered as a singleton on purpose. IHttpClientFactory pools the DelegatingHandler
/// chain for ~2 minutes and resolves handler dependencies outside the current request
/// scope, so a scoped provider here would be a captive dependency serving stale tokens.
/// IHttpContextAccessor is itself a singleton over an AsyncLocal, which makes this safe.
///
/// This depends on tool handlers running on the HTTP request's ExecutionContext, which is what
/// stateless Streamable HTTP does. The stateful escape hatch (HttpServerTransportOptions
/// .PerSessionExecutionContext, obsolete in 2.1.0) detaches them, and every call here would
/// then return null.
/// </remarks>
public sealed class HttpContextTokenProvider(IHttpContextAccessor accessor) : ITokenProvider
{
    private const string BearerPrefix = "Bearer ";

    public ValueTask<string?> GetTokenAsync(CancellationToken ct)
    {
        var header = accessor.HttpContext?.Request.Headers.Authorization.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(header))
            return ValueTask.FromResult<string?>(null);

        var token = header.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase)
            ? header[BearerPrefix.Length..].Trim()
            : header.Trim();

        return ValueTask.FromResult(string.IsNullOrEmpty(token) ? null : token);
    }
}
