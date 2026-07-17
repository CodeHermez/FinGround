using FinGround.Application.AuditLogs.Queries.ReconcileAccount;

namespace FinGround.Application.AuditLogs.Queries.ReconcileAccount;

public record ReconciliationReportDto(
    Guid AccountId,
    string AccountNumber,

    Balance currently stored in the Accounts table.</summary>
    decimal StoredBalance,

    
    /// Balance computed by replaying the audit trail
    /// (last entry's BalanceAfter).  Null when no audit entries exist.
    
    decimal? ComputedBalance,

    StoredBalance – ComputedBalance.  Zero means the books match.</summary>
    decimal? Discrepancy,

    
    /// Reconciled | Discrepancy | NoAuditTrail | TrailGapDetected
    
    string Status,

    int EntryCount,

    Balance recorded before the very first audit entry.</summary>
    decimal? AuditTrailOpeningBalance,

    IReadOnlyList<TrailGapDto> Gaps
);
