namespace FinGround.Application.Health.Queries.GetDetailedHealth;

public record DetailedHealthDto(
    //healthy | degraded | unhealthy
    string Status,
    DateTime CheckedAt,

    //elapsed time since process start, formatted as hh:mm:ss.
    string Uptime,

    DatabaseHealthDto Database,
    ReconciliationHealthDto Reconciliation
);

public record DatabaseHealthDto(
    //healthy | unhealthy
    string Status,
    long LatencyMs,
    IReadOnlyList<string> AppliedMigrations,
    string? LatestMigration
);

public record ReconciliationHealthDto(
    //healthy | degraded
    string Status,
    bool AllClean,
    int TotalAccounts,
    int Reconciled,
    int Discrepancies,
    int TrailGapsDetected,
    int NoAuditTrail
);
