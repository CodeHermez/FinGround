using System.ComponentModel;
using FinGround.McpServer.Api;
using ModelContextProtocol.Server;

namespace FinGround.McpServer.Tools;

[McpServerToolType]
public sealed class AccountTools(FinGroundApiClient api)
{
    [McpServerTool(
        Name = "list_accounts",
        Title = "List bank accounts",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("List FinGround bank accounts with optional filters. Returns each account's id, "
        + "account number and current balance. Use the returned id (a GUID) with the other account tools.")]
    public async Task<PagedToolResult<AccountDto>> ListAccountsAsync(
        [Description("Case-insensitive substring match on the account number, e.g. \"CHK\" or \"0001\".")]
        string? accountNumber = null,
        [Description("Only return accounts with a balance greater than or equal to this.")]
        decimal? minBalance = null,
        [Description("Only return accounts with a balance less than or equal to this.")]
        decimal? maxBalance = null,
        [Description("1-based page number.")] int page = 1,
        [Description("Accounts per page, 1-100.")] int pageSize = 20,
        CancellationToken ct = default)
    {
        var (p, size) = ToolHelpers.NormalisePaging(page, pageSize);
        var result = await api.GetAccountsAsync(accountNumber, minBalance, maxBalance, p, size, ct);
        return result.ToToolResult();
    }

    [McpServerTool(
        Name = "get_account",
        Title = "Get one account",
        ReadOnly = true,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Look up a single bank account by its id and return its account number and current balance.")]
    public Task<AccountDto> GetAccountAsync(
        [Description("The account's GUID, as returned by list_accounts.")] Guid accountId,
        CancellationToken ct = default) =>
        api.GetAccountAsync(accountId, ct);
}
