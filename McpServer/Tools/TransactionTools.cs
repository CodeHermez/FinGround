using System.ComponentModel;
using FinGround.McpServer.Api;
using ModelContextProtocol.Server;

namespace FinGround.McpServer.Tools;

[McpServerToolType]
public sealed class TransactionTools(FinGroundApiClient api)
{
    [McpServerTool(
        Name = "get_account_transactions",
        Title = "Get account transaction history",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Return an account's transaction history, newest first. Includes transfers both "
        + "into and out of the account; compare sourceAccountId and destinationAccountId against "
        + "the account id to tell the direction.")]
    public async Task<PagedToolResult<TransactionDto>> GetAccountTransactionsAsync(
        [Description("The account's GUID, as returned by list_accounts.")]
        Guid accountId,
        [Description("Only return transactions of at least this amount.")]
        decimal? minAmount = null,
        [Description("Only return transactions of at most this amount.")]
        decimal? maxAmount = null,
        [Description("Only return transactions at or after this UTC timestamp (ISO-8601).")]
        DateTime? from = null,
        [Description("Only return transactions at or before this UTC timestamp (ISO-8601).")]
        DateTime? to = null,
        [Description("1-based page number.")] int page = 1,
        [Description("Transactions per page, 1-100.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (p, size) = ToolHelpers.NormalisePaging(page, pageSize);
        var result = await api.GetAccountTransactionsAsync(
            accountId, minAmount, maxAmount, from, to, p, size, ct);
        return result.ToToolResult();
    }
}
