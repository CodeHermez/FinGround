namespace FinGround.McpServer.Api;

/// <summary>
/// Supplies the JWT that <see cref="BearerTokenHandler"/> attaches to outbound API calls.
/// One implementation per transport: the HTTP transport forwards the caller's own token,
/// the stdio transport uses configured credentials or an in-session login.
/// </summary>
public interface ITokenProvider
{
    ValueTask<string?> GetTokenAsync(CancellationToken ct);
}
