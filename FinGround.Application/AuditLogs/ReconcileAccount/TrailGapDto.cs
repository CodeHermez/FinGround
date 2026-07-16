namespace FinGround.Application.AuditLogs.Queries.ReconcileAccount;

public record TrailGapDto(
    Guid EntryId,
    int EntryIndex,
    decimal ExpectedBalanceBefore,
    decimal ActualBalanceBefore,
    decimal Variance
);
