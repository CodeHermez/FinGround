using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Options;
using ModelContextProtocol;

namespace FinGround.McpServer.Api;

/// <summary>Which transport is hosting this process. Only affects the wording of auth errors.</summary>
public enum McpTransport
{
    Http,
    Stdio
}

/// <summary>Wrapper so the transport can be resolved from DI (an enum is not a reference type).</summary>
public sealed record McpTransportContext(McpTransport Transport);

/// <summary>
/// Typed client over the FinGround REST API. One method per endpoint, no business logic —
/// all of that lives in the Application layer behind the API.
/// </summary>
public sealed class FinGroundApiClient(
    HttpClient http,
    IOptions<FinGroundApiOptions> options,
    McpTransportContext transport)
{
    /// <summary>Named client with no bearer handler, used for login so the token lookup cannot recurse.</summary>
    public const string AnonymousClientName = "finground-anon";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly FinGroundApiOptions _options = options.Value;

    // ── Accounts ──────────────────────────────────────────────────────────────

    public Task<PagedResult<AccountDto>> GetAccountsAsync(
        string? accountNumber, decimal? minBalance, decimal? maxBalance,
        int page, int pageSize, CancellationToken ct)
    {
        var query = new QueryBuilder()
            .Add("accountNumber", accountNumber)
            .Add("minBalance", minBalance)
            .Add("maxBalance", maxBalance)
            .Add("page", page)
            .Add("pageSize", pageSize);

        return GetAsync<PagedResult<AccountDto>>($"/api/accounts{query}", ct);
    }

    public Task<AccountDto> GetAccountAsync(Guid accountId, CancellationToken ct) =>
        GetAsync<AccountDto>($"/api/accounts/{accountId}", ct);

    /// <remarks>The API returns a bare Guid in the 201 body, not an object wrapper.</remarks>
    public Task<Guid> CreateAccountAsync(string accountNumber, decimal initialBalance, CancellationToken ct) =>
        PostAsync<Guid>("/api/accounts", new { accountNumber, initialBalance }, ct);

    public Task<BalanceResponse> DepositAsync(Guid accountId, decimal amount, CancellationToken ct) =>
        PostAsync<BalanceResponse>($"/api/accounts/{accountId}/deposit", new { amount }, ct);

    public Task<BalanceResponse> WithdrawAsync(Guid accountId, decimal amount, CancellationToken ct) =>
        PostAsync<BalanceResponse>($"/api/accounts/{accountId}/withdraw", new { amount }, ct);

    // ── Transactions ──────────────────────────────────────────────────────────

    public Task<PagedResult<TransactionDto>> GetAccountTransactionsAsync(
        Guid accountId, decimal? minAmount, decimal? maxAmount,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var query = new QueryBuilder()
            .Add("minAmount", minAmount)
            .Add("maxAmount", maxAmount)
            .Add("from", from)
            .Add("to", to)
            .Add("page", page)
            .Add("pageSize", pageSize);

        return GetAsync<PagedResult<TransactionDto>>($"/api/transactions/account/{accountId}{query}", ct);
    }

    public Task<TransferResponse> TransferAsync(
        Guid sourceAccountId, Guid destinationAccountId, decimal amount, CancellationToken ct) =>
        PostAsync<TransferResponse>(
            "/api/transactions/transfer",
            new { sourceAccountId, destinationAccountId, amount },
            ct);

    // ── Audit logs & reconciliation ───────────────────────────────────────────

    public Task<PagedResult<AuditLogDto>> GetAuditLogsAsync(
        Guid? accountId, string? command, string? initiatedBy,
        DateTime? from, DateTime? to, int page, int pageSize, CancellationToken ct)
    {
        var query = new QueryBuilder()
            .Add("accountId", accountId)
            .Add("command", command)
            .Add("initiatedBy", initiatedBy)
            .Add("from", from)
            .Add("to", to)
            .Add("page", page)
            .Add("pageSize", pageSize);

        return GetAsync<PagedResult<AuditLogDto>>($"/api/auditlogs{query}", ct);
    }

    public Task<PagedResult<AuditLogDto>> GetAccountAuditLogsAsync(
        Guid accountId, string? command, DateTime? from, DateTime? to,
        int page, int pageSize, CancellationToken ct)
    {
        var query = new QueryBuilder()
            .Add("command", command)
            .Add("from", from)
            .Add("to", to)
            .Add("page", page)
            .Add("pageSize", pageSize);

        return GetAsync<PagedResult<AuditLogDto>>($"/api/auditlogs/accounts/{accountId}{query}", ct);
    }

    public Task<ReconciliationReportDto> ReconcileAccountAsync(Guid accountId, CancellationToken ct) =>
        GetAsync<ReconciliationReportDto>($"/api/auditlogs/accounts/{accountId}/reconcile", ct);

    public Task<ReconciliationSweepDto> ReconcileAllAccountsAsync(CancellationToken ct) =>
        GetAsync<ReconciliationSweepDto>("/api/auditlogs/reconcile/all", ct);

    // ── Health ────────────────────────────────────────────────────────────────

    public Task<HealthDto> GetHealthAsync(CancellationToken ct) =>
        GetAsync<HealthDto>("/api/health", ct);

    public Task<DetailedHealthDto> GetDetailedHealthAsync(CancellationToken ct) =>
        GetAsync<DetailedHealthDto>("/api/health/detailed", ct);

    // ── Plumbing ──────────────────────────────────────────────────────────────

    private async Task<T> GetAsync<T>(string path, CancellationToken ct)
    {
        var response = await SendAsync(() => http.GetAsync(path, ct));
        return await ReadAsync<T>(response, ct);
    }

    private async Task<T> PostAsync<T>(string path, object body, CancellationToken ct)
    {
        var response = await SendAsync(() => http.PostAsJsonAsync(path, body, JsonOptions, ct));
        return await ReadAsync<T>(response, ct);
    }

    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        try
        {
            return await send();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            throw ApiProblemExtensions.ToUnreachableException(_options.BaseUrl, ex);
        }
    }

    private async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        await response.ThrowIfUnsuccessfulAsync(transport.Transport == McpTransport.Stdio, ct);

        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions, ct);

        return value ?? throw new McpException(
            $"The FinGround API returned an empty body where a {typeof(T).Name} was expected.");
    }

    /// <summary>Builds a query string, skipping nulls so unset filters are never sent.</summary>
    private sealed class QueryBuilder
    {
        private readonly List<string> _parts = [];

        public QueryBuilder Add(string name, string? value) =>
            string.IsNullOrWhiteSpace(value) ? this : Append(name, value);

        public QueryBuilder Add(string name, decimal? value) =>
            value is null ? this : Append(name, value.Value.ToString(CultureInfo.InvariantCulture));

        public QueryBuilder Add(string name, int value) =>
            Append(name, value.ToString(CultureInfo.InvariantCulture));

        public QueryBuilder Add(string name, Guid? value) =>
            value is null ? this : Append(name, value.Value.ToString());

        public QueryBuilder Add(string name, DateTime? value) =>
            value is null ? this : Append(name, value.Value.ToString("O", CultureInfo.InvariantCulture));

        private QueryBuilder Append(string name, string value)
        {
            _parts.Add($"{Uri.EscapeDataString(name)}={Uri.EscapeDataString(value)}");
            return this;
        }

        public override string ToString() => _parts.Count == 0 ? "" : "?" + string.Join("&", _parts);
    }
}
