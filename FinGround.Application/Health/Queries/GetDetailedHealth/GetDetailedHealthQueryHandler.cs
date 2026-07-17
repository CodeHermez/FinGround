using FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;
using FinGround.Application.Common.Interfaces;
using FinGround.Application.Interfaces;
using FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;
using FinGround.Application.Common.Interfaces;
using FinGround.Application.Health.Queries.GetDetailedHealth;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Health.Queries.GetDetailedHealth;

public class GetDetailedHealthQueryHandler
    : IRequestHandler<GetDetailedHealthQuery, DetailedHealthDto>
{
    private readonly IDatabaseHealthChecker _dbChecker;
    private readonly IMediator _mediator;

    public GetDetailedHealthQueryHandler(
        IDatabaseHealthChecker dbChecker,
        IMediator mediator)
    {
        _dbChecker = dbChecker;
        _mediator = mediator;
    }

    public async Task<DetailedHealthDto> Handle(
        GetDetailedHealthQuery request, CancellationToken cancellationToken)
    {
        var checkedAt = DateTime.UtcNow;

        // Uptime
        var elapsed = DateTime.UtcNow
                    - System.Diagnostics.Process.GetCurrentProcess()
                                                .StartTime.ToUniversalTime();
        var uptime = elapsed.ToString(@"hh\:mm\:ss");

        // DB
        var dbResult = await _dbChecker.CheckAsync(cancellationToken);

        var dbDto = new DatabaseHealthDto(
            Status: dbResult.IsConnected ? "Healthy" : "Unhealthy",
            LatencyMs: dbResult.LatencyMs,
            AppliedMigrations: dbResult.AppliedMigrations,
            LatestMigration: dbResult.AppliedMigrations.Count > 0
                                   ? dbResult.AppliedMigrations[^1]
                                   : null);

        //reconciliation sweep
        ReconciliationHealthDto recoDto;

        if (!dbResult.IsConnected)
        {
            recoDto = new ReconciliationHealthDto(
                Status: "Unhealthy",
                AllClean: false,
                TotalAccounts: 0,
                Reconciled: 0,
                Discrepancies: 0,
                TrailGapsDetected: 0,
                NoAuditTrail: 0);
        }
        else
        {
            var sweep = await _mediator.Send(new ReconcileAllAccountsQuery(), cancellationToken);

            recoDto = new ReconciliationHealthDto(
                Status: sweep.AllClean ? "Healthy" : "Degraded",
                AllClean: sweep.AllClean,
                TotalAccounts: sweep.TotalAccounts,
                Reconciled: sweep.Reconciled,
                Discrepancies: sweep.Discrepancies,
                TrailGapsDetected: sweep.TrailGapsDetected,
                NoAuditTrail: sweep.NoAuditTrail);
        }

        //Overall status
        var overallStatus = !dbResult.IsConnected ? "Unhealthy"
                          : !recoDto.AllClean ? "Degraded"
                          : "Healthy";

        return new DetailedHealthDto(
            Status: overallStatus,
            CheckedAt: checkedAt,
            Uptime: uptime,
            Database: dbDto,
            Reconciliation: recoDto);
    }
}
