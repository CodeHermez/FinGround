using FinGround.Application.AuditLogs.Queries.GetAllAuditLogs;
using FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;
using FinGround.Application.AuditLogs.Queries.ReconcileAccount;
using FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;
using FinGround.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinGround.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AuditLogsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuditLogsController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns a paginated, optionally-filtered list of all audit log entries
    /// across every account, newest first.
    ///
    /// All filter parameters are optional and combinable:
    /// - accountId:   restrict to a single account's entries
    /// - command:     case-insensitive substring match ("Deposit", "Transfer", etc.)
    /// - initiatedBy: case-insensitive substring match on the user email
    /// - from:        entries on or after this UTC timestamp (ISO 8601)
    /// - to:          entries on or before this UTC timestamp (ISO 8601)
    ///
    /// Pagination defaults: page = 1, pageSize = 50, maximum pageSize = 100.
    /// </summary>
    /// <param name="accountId">Filter to a specific account ID.</param>
    /// <param name="command">Substring to match against command name.</param>
    /// <param name="initiatedBy">Substring to match against the initiating user's email.</param>
    /// <param name="from">Start of date range, UTC.</param>
    /// <param name="to">End of date range, UTC.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 50).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? accountId = null,
        [FromQuery] string? command = null,
        [FromQuery] string? initiatedBy = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAllAuditLogsQuery(accountId, command, initiatedBy, from, to, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated audit trail for a specific account, newest first.
    /// Each deposit, withdrawal, and transfer leg is recorded with
    /// before/after balance snapshots.
    ///
    /// All filter parameters are optional and combinable:
    /// - command: case-insensitive substring match ("Deposit", "Transfer", etc.)
    /// - from:    entries on or after this UTC timestamp (ISO 8601)
    /// - to:      entries on or before this UTC timestamp (ISO 8601)
    ///
    /// Pagination defaults: page = 1, pageSize = 50, maximum pageSize = 100.
    /// </summary>
    /// <param name="accountId">Account whose audit trail to retrieve.</param>
    /// <param name="command">Substring to match against command name.</param>
    /// <param name="from">Start of date range, UTC.</param>
    /// <param name="to">End of date range, UTC.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 50).</param>
    [HttpGet("accounts/{accountId:guid}")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAccount(
        Guid accountId,
        [FromQuery] string? command = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsByAccountQuery(accountId, command, from, to, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Runs the balance-integrity check across every account in one call.
    /// Returns a sweep summary (counts by status, AllClean flag) plus the
    /// full per-account reconciliation report for every account.
    /// The two database queries (accounts + audit logs) are batched —
    /// cost is O(accounts + audit_log_rows), not O(N × accounts).
    /// </summary>
    [HttpGet("reconcile/all")]
    [ProducesResponseType(typeof(ReconciliationSweepDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ReconcileAll(CancellationToken ct)
    {
        var sweep = await _mediator.Send(new ReconcileAllAccountsQuery(), ct);
        return Ok(sweep);
    }

    /// <summary>
    /// Replays an account's audit trail and verifies that each entry's opening
    /// balance matches the previous entry's closing balance, and that the final
    /// computed balance matches the value stored in the Accounts table.
    ///
    /// Possible status values:
    /// - Reconciled          — trail is unbroken and matches the stored balance.
    /// - Discrepancy         — trail is unbroken but the final balance does not match.
    /// - TrailGapDetected    — one or more consecutive entries have mismatched balances.
    /// - NoAuditTrail        — no audit entries exist yet for this account.
    /// </summary>
    [HttpGet("accounts/{accountId:guid}/reconcile")]
    [ProducesResponseType(typeof(ReconciliationReportDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Reconcile(Guid accountId, CancellationToken ct)
    {
        var report = await _mediator.Send(new ReconcileAccountQuery(accountId), ct);
        return Ok(report);
    }
}
