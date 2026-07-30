using FinGround.Application.AuditLogs.Queries.ReconcileAccount;

namespace FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;

public record AccountReconciliationDto(
    Guid AccountId,
    string AccountNumber,
    decimal StoredBalance,
    decimal? ComputedBalance,
    decimal? Discrepancy,
    string Status,
    int EntryCount,
    decimal? AuditTrailOpeningBalance,
    IReadOnlyList<TrailGapDto> Gaps
);
