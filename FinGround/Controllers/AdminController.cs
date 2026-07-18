using FinGround.Application.Admin.Commands.UnlockAccount;
using FinGround.Application.Admin.Queries.GetAllUsers;
using FinGround.Application.Admin.Queries.GetUserDetail;
using FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;
using FinGround.Application.Common.Models;
using FinGround.Application.Transactions.Queries.GetTransactionsByAccount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FinGround.API.Controllers;

/// <summary>
/// Administrative operations restricted to users with the Admin role.
///
/// The demo seed account (demo@banking-sandbox.dev) carries the Admin role
/// in its JWT.  All other self-registered accounts receive the User role
/// and will receive 403 Forbidden from this controller.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Returns a paginated, optionally-filtered list of users.
    ///
    /// All filter parameters are optional and combinable:
    /// - role: exact match, case-insensitive ("Admin" | "User")
    /// - isLocked: true = only locked accounts, false = only unlocked accounts
    ///
    /// Pagination defaults: page = 1, pageSize = 20, maximum pageSize = 100.
    /// The response envelope includes totalCount, totalPages, hasNextPage,
    /// and hasPreviousPage so clients never need to hard-code limits.
    /// </summary>
    /// <param name="role">Filter by role ("Admin" or "User"). Omit to return all roles.</param>
    /// <param name="isLocked">Filter by lockout state. Omit to return all states.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 20).</param>
    [HttpGet("users")]
    [ProducesResponseType(typeof(PagedResult<UserSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] string? role     = null,
        [FromQuery] bool?   isLocked = null,
        [FromQuery] int     page     = 1,
        [FromQuery] int     pageSize = 20,
        CancellationToken   ct       = default)
    {
        var result = await _mediator.Send(
            new GetAllUsersQuery(role, isLocked, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns full detail for a single user: profile fields, lockout state,
    /// and their complete audit trail across all accounts — newest entries first.
    /// Use this to review a user's activity history before deciding to unlock.
    /// </summary>
    /// <param name="userId">The GUID of the user to inspect.</param>
    [HttpGet("users/{userId:guid}")]
    [ProducesResponseType(typeof(UserDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUser(Guid userId, CancellationToken ct)
    {
        var user = await _mediator.Send(new GetUserDetailQuery(userId), ct);
        return Ok(user);
    }

    /// <summary>
    /// Unlocks a user account that was temporarily locked after too many
    /// failed login attempts. Resets the failed-attempt counter to zero
    /// and clears the LockedUntil timestamp.
    /// </summary>
    /// <param name="userId">The GUID of the user to unlock.</param>
    [HttpPost("users/{userId:guid}/unlock")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UnlockUser(Guid userId, CancellationToken ct)
    {
        await _mediator.Send(new UnlockAccountCommand(userId), ct);
        return NoContent();
    }

    // Account inspection
    // Dedicated admin routes so ownership-scoped user endpoints can be tightened
    // in future without removing admin visibility.

    /// <summary>
    /// Returns a paginated, optionally-filtered list of transactions for any
    /// account — regardless of which user owns it.
    ///
    /// Identical filter and pagination contract to
    /// <c>GET /api/transactions/account/{accountId}</c>, but restricted to
    /// the Admin role so it can be called without the owning user's JWT.
    ///
    /// All filter parameters are optional and combinable:
    /// - minAmount: transactions with Amount &gt;= this value
    /// - maxAmount: transactions with Amount &lt;= this value
    /// - from: transactions on or after this UTC timestamp (ISO 8601)
    /// - to: transactions on or before this UTC timestamp (ISO 8601)
    ///
    /// Pagination defaults: page = 1, pageSize = 20, maximum pageSize = 100.
    /// Returns 404 if the account does not exist.
    /// </summary>
    /// <param name="accountId">The account to inspect.</param>
    /// <param name="minAmount">Minimum transaction amount (inclusive).</param>
    /// <param name="maxAmount">Maximum transaction amount (inclusive).</param>
    /// <param name="from">Start of date range, UTC (e.g. 2026-01-01T00:00:00Z).</param>
    /// <param name="to">End of date range, UTC (e.g. 2026-12-31T23:59:59Z).</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 20).</param>
    [HttpGet("accounts/{accountId:guid}/transactions")]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountTransactions(
        Guid              accountId,
        [FromQuery] decimal?  minAmount = null,
        [FromQuery] decimal?  maxAmount = null,
        [FromQuery] DateTime? from      = null,
        [FromQuery] DateTime? to        = null,
        [FromQuery] int       page      = 1,
        [FromQuery] int       pageSize  = 20,
        CancellationToken     ct        = default)
    {
        var result = await _mediator.Send(
            new GetTransactionsByAccountQuery(accountId, minAmount, maxAmount, from, to, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a paginated, optionally-filtered audit trail for any account —
    /// regardless of which user owns it.
    ///
    /// Identical filter and pagination contract to
    /// <c>GET /api/auditlogs/accounts/{accountId}</c>, but restricted to
    /// the Admin role so it can be called without the owning user's JWT.
    ///
    /// All filter parameters are optional and combinable:
    /// - command: case-insensitive substring match ("Deposit", "Transfer", etc.)
    /// - from: entries on or after this UTC timestamp (ISO 8601)
    /// - to: entries on or before this UTC timestamp (ISO 8601)
    ///
    /// Pagination defaults: page = 1, pageSize = 50, maximum pageSize = 100.
    /// Returns 404 if the account does not exist.
    /// </summary>
    /// <param name="accountId">The account whose audit trail to retrieve.</param>
    /// <param name="command">Substring to match against command name.</param>
    /// <param name="from">Start of date range, UTC.</param>
    /// <param name="to">End of date range, UTC.</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 50).</param>
    [HttpGet("accounts/{accountId:guid}/auditlogs")]
    [ProducesResponseType(typeof(PagedResult<AuditLogDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAccountAuditLogs(
        Guid              accountId,
        [FromQuery] string?   command  = null,
        [FromQuery] DateTime? from     = null,
        [FromQuery] DateTime? to       = null,
        [FromQuery] int       page     = 1,
        [FromQuery] int       pageSize = 50,
        CancellationToken     ct       = default)
    {
        var result = await _mediator.Send(
            new GetAuditLogsByAccountQuery(accountId, command, from, to, page, pageSize), ct);
        return Ok(result);
    }
}
