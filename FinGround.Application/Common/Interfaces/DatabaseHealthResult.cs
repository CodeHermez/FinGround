namespace FinGround.Application.Common.Interfaces;

public record DatabaseHealthResult(
    bool IsConnected,
    long LatencyMs,
    IReadOnlyList<string> AppliedMigrations
);

public interface IDatabaseHealthChecker
{
    Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default);
}
