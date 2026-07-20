namespace FinGround.Application.Transactions.Queries.GetTransactionsByAccount;

public record TransactionDto(
    Guid Id,
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    DateTime Timestamp
);
