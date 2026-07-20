using FinGround.Application.Common.Models;
using FinGround.Application.Transactions.Commands.TransferFunds;
using FinGround.Application.Transactions.Queries.GetTransactionsByAccount;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinGround.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TransactionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TransactionsController(IMediator mediator) => _mediator = mediator;

    private string? CallerEmail =>
        User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("email")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Returns a paginated list of transactions where the given account is
    /// either the source or the destination, newest first.
    ///
    /// All filter parameters are optional and combinable:
    /// - minAmount: only transactions with Amount >= this value
    /// - maxAmount: only transactions with Amount &lt;= this value
    /// - from: only transactions on or after this UTC timestamp (ISO 8601)
    /// - to: only transactions on or before this UTC timestamp (ISO 8601)
    ///
    /// Pagination defaults: page = 1, pageSize = 20, maximum pageSize = 100.
    /// </summary>
    /// <param name="accountId">Account to query (source or destination).</param>
    /// <param name="minAmount">Minimum transaction amount (inclusive).</param>
    /// <param name="maxAmount">Maximum transaction amount (inclusive).</param>
    /// <param name="from">Start of date range, UTC (e.g. 2026-01-01T00:00:00Z).</param>
    /// <param name="to">End of date range, UTC (e.g. 2026-12-31T23:59:59Z).</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 20).</param>
    [HttpGet("account/{accountId:guid}")]
    [ProducesResponseType(typeof(PagedResult<TransactionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByAccount(
        Guid accountId,
        [FromQuery] decimal? minAmount = null,
        [FromQuery] decimal? maxAmount = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetTransactionsByAccountQuery(accountId, minAmount, maxAmount, from, to, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Atomically transfers funds from one account to another.
    /// Both balances and the transaction record are persisted in a single
    /// SaveChanges call.
    /// </summary>
    [HttpPost("transfer")]
    [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Transfer(
        [FromBody] TransferRequest request, CancellationToken ct)
    {
        var transactionId = await _mediator.Send(
            new TransferFundsCommand(
                request.SourceAccountId,
                request.DestinationAccountId,
                request.Amount,
                CallerEmail),
            ct);

        return StatusCode(
            StatusCodes.Status201Created,
            new TransferResponse(transactionId));
    }
}

//Request/Response models

public record TransferRequest(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount);

public record TransferResponse(Guid TransactionId);
