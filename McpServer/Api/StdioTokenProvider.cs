using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace FinGround.McpServer.Api;

/// <summary>
/// stdio transport: there is no per-request Authorization header, so the token comes from
/// (in order) an in-session login, a pre-issued token in config, or an automatic login with
/// configured credentials.
/// </summary>
/// <remarks>
/// Singleton — see the note on <see cref="HttpContextTokenProvider"/>. Login goes through the
/// "finground-anon" named client, which has no <see cref="BearerTokenHandler"/> in its chain;
/// using the authenticated client here would recurse.
///
/// POST /api/auth/login is rate limited by the API's "auth-login" token bucket (5 burst,
/// refilling 1 per 12s, partitioned per client IP). Tokens are therefore cached against their
/// expiry and concurrent refreshes are serialised, so a burst of tool calls triggers one login.
/// </remarks>
public sealed class StdioTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<FinGroundApiOptions> options,
    ILogger<StdioTokenProvider> logger) : ITokenProvider
{
    /// <summary>Refresh this long before the token actually expires, to cover clock skew and flight time.</summary>
    private static readonly TimeSpan ExpirySlack = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly FinGroundApiOptions _options = options.Value;

    private string? _token;
    private DateTime _expiresAtUtc = DateTime.MinValue;

    /// <summary>Set by the login tool. Returns the caller identity for confirmation, never the token.</summary>
    public async Task<AuthResultDto> LoginAsync(string email, string password, CancellationToken ct)
    {
        var result = await AuthenticateAsync(email, password, ct);

        await _gate.WaitAsync(ct);
        try
        {
            _token = result.Token;
            _expiresAtUtc = DateTime.SpecifyKind(result.ExpiresAt, DateTimeKind.Utc);
        }
        finally
        {
            _gate.Release();
        }

        return result;
    }

    public async ValueTask<string?> GetTokenAsync(CancellationToken ct)
    {
        if (IsCachedTokenUsable())
            return _token;

        // A pre-issued token wins over credentials: it is what the operator explicitly supplied.
        if (!string.IsNullOrWhiteSpace(_options.BearerToken))
            return _options.BearerToken;

        if (string.IsNullOrWhiteSpace(_options.Email) || string.IsNullOrWhiteSpace(_options.Password))
            return null;

        await _gate.WaitAsync(ct);
        try
        {
            // Another caller may have refreshed while we waited on the gate.
            if (IsCachedTokenUsable())
                return _token;

            var result = await AuthenticateAsync(_options.Email, _options.Password, ct);
            _token = result.Token;
            _expiresAtUtc = DateTime.SpecifyKind(result.ExpiresAt, DateTimeKind.Utc);

            logger.LogInformation(
                "Authenticated against the FinGround API as {Email}; token valid until {ExpiresAt:u}.",
                result.Email, _expiresAtUtc);

            return _token;
        }
        finally
        {
            _gate.Release();
        }
    }

    private bool IsCachedTokenUsable() =>
        _token is not null && DateTime.UtcNow < _expiresAtUtc - ExpirySlack;

    private async Task<AuthResultDto> AuthenticateAsync(string email, string password, CancellationToken ct)
    {
        var client = httpClientFactory.CreateClient(FinGroundApiClient.AnonymousClientName);

        HttpResponseMessage response;
        try
        {
            response = await client.PostAsJsonAsync(
                "/api/auth/login", new { email, password }, JsonOptions, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw ApiProblemExtensions.ToUnreachableException(_options.BaseUrl, ex);
        }

        await response.ThrowIfUnsuccessfulAsync(isStdio: true, ct);

        return await response.Content.ReadFromJsonAsync<AuthResultDto>(JsonOptions, ct)
               ?? throw new ModelContextProtocol.McpException(
                   "The FinGround API returned an empty response to the login request.");
    }
}
