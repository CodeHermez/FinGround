using System.Net.Http.Headers;

namespace FinGround.McpServer.Api;

/// <summary>
/// Attaches the JWT supplied by the active <see cref="ITokenProvider"/> to every outbound call.
/// When no token is available the request still goes out unauthenticated — /api/health works that
/// way, and everything else returns a 401 that <see cref="ApiProblemExtensions"/> turns into an
/// actionable message rather than a silent failure here.
/// </summary>
public sealed class BearerTokenHandler(ITokenProvider tokenProvider) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await tokenProvider.GetTokenAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return await base.SendAsync(request, cancellationToken);
    }
}
