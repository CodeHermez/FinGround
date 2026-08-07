using System.ComponentModel;
using FinGround.McpServer.Api;
using ModelContextProtocol.Server;

namespace FinGround.McpServer.Tools;

/// <summary>Read-only visibility into the audit trail, balance integrity and service health.</summary>
[McpServerToolType]
public sealed class AuditTools(FinGroundApiClient api)
{
    [McpServerTool(
        Name = "list_audit_logs",
        Title = "Search the audit trail",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Search the global audit trail across all accounts. Every balance change writes an "
        + "entry recording the balance before and after, the amount, and who initiated it. "
        + "Known command values: CreateAccount, Deposit, Withdraw, TransferFunds:Debit, TransferFunds:Credit.")]
    public async Task<PagedToolResult<AuditLogDto>> ListAuditLogsAsync(
        [Description("Restrict to a single account's entries.")] Guid? accountId = null,
        [Description("Exact command name, e.g. \"Deposit\" or \"TransferFunds:Debit\".")] string? command = null,
        [Description("Email of the user who initiated the operation.")] string? initiatedBy = null,
        [Description("Only return entries at or after this UTC timestamp (ISO-8601).")] DateTime? from = null,
        [Description("Only return entries at or before this UTC timestamp (ISO-8601).")] DateTime? to = null,
        [Description("1-based page number.")] int page = 1,
        [Description("Entries per page, 1-100.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (p, size) = ToolHelpers.NormalisePaging(page, pageSize);
        var result = await api.GetAuditLogsAsync(accountId, command, initiatedBy, from, to, p, size, ct);
        return result.ToToolResult();
    }

    [McpServerTool(
        Name = "get_account_audit_logs",
        Title = "Get one account's audit trail",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Return the audit trail for a single account, newest first. Use this to see exactly "
        + "how a balance reached its current value, and who caused each change.")]
    public async Task<PagedToolResult<AuditLogDto>> GetAccountAuditLogsAsync(
        [Description("The account's GUID, as returned by list_accounts.")] Guid accountId,
        [Description("Exact command name, e.g. \"Deposit\".")] string? command = null,
        [Description("Only return entries at or after this UTC timestamp (ISO-8601).")] DateTime? from = null,
        [Description("Only return entries at or before this UTC timestamp (ISO-8601).")] DateTime? to = null,
        [Description("1-based page number.")] int page = 1,
        [Description("Entries per page, 1-100.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (p, size) = ToolHelpers.NormalisePaging(page, pageSize);
        var result = await api.GetAccountAuditLogsAsync(accountId, command, from, to, p, size, ct);
        return result.ToToolResult();
    }

    [McpServerTool(
        Name = "reconcile_account",
        Title = "Reconcile one account",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Replay one account's audit trail and check that the stored balance matches what the "
        + "trail implies. Status is one of: Reconciled (balance agrees), Discrepancy (stored balance "
        + "disagrees with the replay), TrailGapDetected (consecutive entries do not line up), or "
        + "NoAuditTrail (no entries to replay).")]
    public Task<ReconciliationReportDto> ReconcileAccountAsync(
        [Description("The account's GUID, as returned by list_accounts.")] Guid accountId,
        CancellationToken ct = default) =>
        api.ReconcileAccountAsync(accountId, ct);

    [McpServerTool(
        Name = "reconcile_all_accounts",
        Title = "Reconcile every account",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Run the balance-integrity sweep across every account and report how many reconciled "
        + "cleanly. By default returns only the totals and the accounts that failed; set summaryOnly "
        + "to false to include every account's report.")]
    public async Task<ReconciliationSweepResult> ReconcileAllAccountsAsync(
        [Description("Return only the totals and the accounts listed by problemsOnly, omitting clean accounts.")]
        bool summaryOnly = true,
        [Description("When listing accounts, include only those whose status is not \"Reconciled\".")]
        bool problemsOnly = true,
        CancellationToken ct = default)
    {
        var sweep = await api.ReconcileAllAccountsAsync(ct);

        // The raw sweep returns a full report, gap array included, for every account. Trimming here
        // keeps a routine "is everything OK?" check from costing thousands of tokens.
        var accounts = sweep.Accounts.AsEnumerable();

        if (problemsOnly)
            accounts = accounts.Where(a => !string.Equals(a.Status, "Reconciled", StringComparison.OrdinalIgnoreCase));

        IReadOnlyList<object> listed = summaryOnly
            ? accounts.Select(a => (object)new AccountReconciliationSummary(
                a.AccountId, a.AccountNumber, a.Status, a.StoredBalance, a.ComputedBalance, a.Discrepancy)).ToArray()
            : accounts.Cast<object>().ToArray();

        return new ReconciliationSweepResult(
            sweep.RunAt,
            sweep.TotalAccounts,
            sweep.Reconciled,
            sweep.Discrepancies,
            sweep.TrailGapsDetected,
            sweep.NoAuditTrail,
            sweep.AllClean,
            listed,
            summaryOnly || problemsOnly
                ? "Account list is filtered. Call reconcile_account with a specific accountId for the full report including trail gaps."
                : null);
    }

    [McpServerTool(
        Name = "get_health",
        Title = "Check API health",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Check whether the FinGround API and its database are up. Requires no authentication. "
        + "Set detailed to true to also get database latency, applied migrations and the global "
        + "reconciliation status.")]
    public async Task<object> GetHealthAsync(
        [Description("Include database and reconciliation detail. Verbose — leave false for a simple up/down check.")]
        bool detailed = false,
        CancellationToken ct = default) =>
        detailed
            ? await api.GetDetailedHealthAsync(ct)
            : await api.GetHealthAsync(ct);
}

public sealed record AccountReconciliationSummary(
    Guid AccountId,
    string AccountNumber,
    string Status,
    decimal StoredBalance,
    decimal? ComputedBalance,
    decimal? Discrepancy);

public sealed record ReconciliationSweepResult(
    DateTime RunAt,
    int TotalAccounts,
    int Reconciled,
    int Discrepancies,
    int TrailGapsDetected,
    int NoAuditTrail,
    bool AllClean,
    IReadOnlyList<object> Accounts,
    string? Hint);
