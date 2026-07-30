namespace FinGround.Application.AuditLogs.Queries.ReconcileAccount;

public record ReconciliationReportDto(
    Guid AccountId,
    string AccountNumber,

    /// <summary>Balance currently stored in the Accounts table.</summary>
    decimal StoredBalance,

    /// <summary>
    /// Balance computed by replaying the audit trail
    /// (last entry's BalanceAfter).  Null when no audit entries exist.
    /// </summary>
    decimal? ComputedBalance,

    /// <summary>StoredBalance – ComputedBalance.  Zero means the books match.</summary>
    decimal? Discrepancy,

    /// <summary>
    /// Reconciled | Discrepancy | NoAuditTrail | TrailGapDetected
    /// </summary>
    string Status,

    int EntryCount,

    /// <summary>Balance recorded before the very first audit entry.</summary>
    decimal? AuditTrailOpeningBalance,

    IReadOnlyList<TrailGapDto> Gaps
);
