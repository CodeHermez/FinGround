namespace FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;

public record AuditLogDto(
    Guid Id,
    DateTime Timestamp,
    string Command,
    Guid AccountId,
    decimal BalanceBefore,
    decimal BalanceAfter,
    decimal Amount,
    string? InitiatedBy,
    string? Notes
);
