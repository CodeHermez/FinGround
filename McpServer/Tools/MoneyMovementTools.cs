using System.ComponentModel;
using FinGround.McpServer.Api;
using Microsoft.Extensions.Options;
using ModelContextProtocol.Server;

namespace FinGround.McpServer.Tools;

/// <summary>
/// State-changing tools. Registered only when McpServer:EnableMoneyMovement is true, so a
/// read-only deployment omits them from tools/list entirely rather than failing at call time.
/// Every amount is additionally checked against McpServer:MaxTransactionAmount before the call
/// leaves this process.
/// </summary>
[McpServerToolType]
public sealed class MoneyMovementTools(FinGroundApiClient api, IOptions<McpServerSettings> options)
{
    private readonly decimal _maxAmount = options.Value.MaxTransactionAmount;

    [McpServerTool(
        Name = "create_account",
        Title = "Open a bank account",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Open a new bank account with an optional starting balance. Account numbers must be "
        + "unique; the existing sandbox accounts follow a CHK-000001 / SAV-000001 pattern. "
        + "This is recorded in the audit trail against the authenticated caller.")]
    public async Task<CreatedAccountResult> CreateAccountAsync(
        [Description("Unique account number, max 20 characters, e.g. \"CHK-000002\".")]
        string accountNumber,
        [Description("Opening balance. Must not be negative.")]
        decimal initialBalance = 0,
        CancellationToken ct = default)
    {
        if (initialBalance < 0)
            throw new ModelContextProtocol.McpException(
                $"The opening balance cannot be negative (got {initialBalance}).");

        if (initialBalance > _maxAmount)
            throw new ModelContextProtocol.McpException(
                $"The opening balance {initialBalance} exceeds this MCP server's configured limit of "
                + $"{_maxAmount}. Raise McpServer:MaxTransactionAmount to allow larger amounts.");

        var id = await api.CreateAccountAsync(accountNumber, initialBalance, ct);
        return new CreatedAccountResult(id, accountNumber, initialBalance);
    }

    [McpServerTool(
        Name = "deposit",
        Title = "Deposit into an account",
        ReadOnly = false,
        Destructive = false,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Credit money to an account. This permanently increases the balance and writes an "
        + "audit-trail entry attributed to the authenticated caller. Not idempotent — calling it "
        + "twice deposits twice.")]
    public async Task<BalanceChangeResult> DepositAsync(
        [Description("The account's GUID, as returned by list_accounts.")] Guid accountId,
        [Description("Amount to credit. Must be greater than zero.")] decimal amount,
        CancellationToken ct = default)
    {
        ToolHelpers.ValidateAmount(amount, _maxAmount, "deposit");

        var result = await api.DepositAsync(accountId, amount, ct);
        return new BalanceChangeResult(result.AccountId, "Deposit", amount, result.NewBalance);
    }

    [McpServerTool(
        Name = "withdraw",
        Title = "Withdraw from an account",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Debit money from an account. This permanently reduces the balance and writes an "
        + "audit-trail entry attributed to the authenticated caller. Fails if the account has "
        + "insufficient funds. Not idempotent — calling it twice withdraws twice. Confirm the "
        + "account with get_account first.")]
    public async Task<BalanceChangeResult> WithdrawAsync(
        [Description("The account's GUID, as returned by list_accounts.")] Guid accountId,
        [Description("Amount to debit. Must be greater than zero and within the available balance.")]
        decimal amount,
        CancellationToken ct = default)
    {
        ToolHelpers.ValidateAmount(amount, _maxAmount, "withdrawal");

        var result = await api.WithdrawAsync(accountId, amount, ct);
        return new BalanceChangeResult(result.AccountId, "Withdraw", amount, result.NewBalance);
    }

    [McpServerTool(
        Name = "transfer",
        Title = "Transfer between accounts",
        ReadOnly = false,
        Destructive = true,
        Idempotent = false,
        OpenWorld = false)]
    [Description("Atomically move money from one account to another. This permanently changes both "
        + "balances and writes two audit-trail entries (TransferFunds:Debit and TransferFunds:Credit) "
        + "attributed to the authenticated caller. Fails if the source has insufficient funds. Not "
        + "idempotent. Verify both account ids with get_account before calling.")]
    public async Task<TransferResult> TransferAsync(
        [Description("GUID of the account to debit.")] Guid sourceAccountId,
        [Description("GUID of the account to credit.")] Guid destinationAccountId,
        [Description("Amount to move. Must be greater than zero.")] decimal amount,
        CancellationToken ct = default)
    {
        ToolHelpers.ValidateAmount(amount, _maxAmount, "transfer");

        if (sourceAccountId == destinationAccountId)
            throw new ModelContextProtocol.McpException(
                "The source and destination accounts must be different.");

        var result = await api.TransferAsync(sourceAccountId, destinationAccountId, amount, ct);
        return new TransferResult(result.TransactionId, sourceAccountId, destinationAccountId, amount);
    }
}

public sealed record CreatedAccountResult(Guid AccountId, string AccountNumber, decimal Balance);

public sealed record BalanceChangeResult(Guid AccountId, string Operation, decimal Amount, decimal NewBalance);

public sealed record TransferResult(
    Guid TransactionId, Guid SourceAccountId, Guid DestinationAccountId, decimal Amount);
