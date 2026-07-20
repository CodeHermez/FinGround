using FinGround.Application.Health.Queries.GetDetailedHealth;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FinGround.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly IMediator _mediator;

    public HealthController(IMediator mediator) => _mediator = mediator;

    /// <summary>Simple liveness probe — no auth required.</summary>
    [HttpGet]
    public IActionResult Get() =>
        Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });

    /// <summary>
    /// Deep health check — no auth required so monitoring tools can call it freely.
    ///
    /// Reports three subsystems:
    ///
    /// database — connectivity ping + round-trip latency + list of applied EF migrations.
    ///
    /// reconciliation — runs the global audit-log sweep and reports whether every
    /// account's stored balance matches its replayed audit trail.
    ///
    /// Overall status:
    ///   Healthy   — DB reachable and all accounts reconciled.
    ///   Degraded  — DB reachable but one or more accounts have trail gaps or discrepancies.
    ///   Unhealthy — DB unreachable.
    /// </summary>
    [HttpGet("detailed")]
    [ProducesResponseType(typeof(DetailedHealthDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(DetailedHealthDto), StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Detailed(CancellationToken ct)
    {
        var report = await _mediator.Send(new GetDetailedHealthQuery(), ct);

        var httpStatus = report.Status == "Unhealthy"
            ? StatusCodes.Status503ServiceUnavailable
            : StatusCodes.Status200OK;

        return StatusCode(httpStatus, report);
    }
}
