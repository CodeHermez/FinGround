using FinGround.Application.Common.Interfaces;
using FinGround.Infrastructure.Persistence;
using FinGround.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;

namespace FinGround.Infrastructure.Services;

public class DatabaseHealthChecker : IDatabaseHealthChecker
{
    private readonly BankDbContext _context;

    public DatabaseHealthChecker(BankDbContext context) => _context = context;

    public async Task<DatabaseHealthResult> CheckAsync(CancellationToken cancellationToken = default)
    {
        var sw = Stopwatch.StartNew();
        bool connected;

        try
        {
            connected = await _context.Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            sw.Stop();
            return new DatabaseHealthResult(
                IsConnected: false,
                LatencyMs: sw.ElapsedMilliseconds,
                AppliedMigrations: Array.Empty<string>());
        }

        sw.Stop();

        if (!connected)
        {
            return new DatabaseHealthResult(
                IsConnected: false,
                LatencyMs: sw.ElapsedMilliseconds,
                AppliedMigrations: Array.Empty<string>());
        }

        // read applied migrations via raw ADO.NET
        var migrations = new List<string>();

        try
        {
            var connection = _context.Database.GetDbConnection();
            await connection.OpenAsync(cancellationToken);

            await using var cmd = connection.CreateCommand();
            cmd.CommandText =
                "SELECT \"MigrationId\" FROM \"__EFMigrationsHistory\" ORDER BY \"MigrationId\"";

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                migrations.Add(reader.GetString(0));

            await connection.CloseAsync();
        }
        catch
        {
            // not critical, return what we have if anything
        }

        return new DatabaseHealthResult(
            IsConnected: true,
            LatencyMs: sw.ElapsedMilliseconds,
            AppliedMigrations: migrations);
    }
}
