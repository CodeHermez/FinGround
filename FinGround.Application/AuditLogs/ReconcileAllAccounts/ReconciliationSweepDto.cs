namespace FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;

public record ReconciliationSweepDto(
    DateTime RunAt,
    int TotalAccounts,
    int Reconciled,
    int Discrepancies,
    int TrailGapsDetected,
    int NoAuditTrail,

    //true when every account has status Reconciled
    bool AllClean,

    IReadOnlyList<AccountReconciliationDto> Accounts
);
