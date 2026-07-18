using FinGround.Application.Accounts.Commands.CreateAccount;
using FinGround.Application.Accounts.Commands.Deposit;
using FinGround.Application.Accounts.Commands.Withdraw;
using FinGround.Application.Accounts.Queries.GetAccountById;
using FinGround.Application.Accounts.Queries.GetAllAccounts;
using FinGround.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FinGround.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AccountsController(IMediator mediator) => _mediator = mediator;

    private string? CallerEmail =>
        User.FindFirst(ClaimTypes.Email)?.Value
        ?? User.FindFirst("email")?.Value
        ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

    /// <summary>
    /// Returns a paginated, optionally-filtered list of accounts.
    ///
    /// All filter parameters are optional and combinable:
    /// - accountNumber: case-insensitive substring match ("CHK", "SAV", "0001", etc.)
    /// - minBalance: only accounts with Balance >= this value
    /// - maxBalance: only accounts with Balance &lt;= this value
    ///
    /// Pagination defaults: page = 1, pageSize = 20, maximum pageSize = 100.
    /// </summary>
    /// <param name="accountNumber">Substring to match against account number (case-insensitive).</param>
    /// <param name="minBalance">Minimum balance (inclusive).</param>
    /// <param name="maxBalance">Maximum balance (inclusive).</param>
    /// <param name="page">1-based page number (default: 1).</param>
    /// <param name="pageSize">Items per page, 1–100 (default: 20).</param>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<AccountDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? accountNumber = null,
        [FromQuery] decimal? minBalance = null,
        [FromQuery] decimal? maxBalance = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(
            new GetAllAccountsQuery(accountNumber, minBalance, maxBalance, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>Returns a single account by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAccountByIdQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Opens a new bank account.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateAccountRequest request, CancellationToken ct)
    {
        var id = await _mediator.Send(
            new CreateAccountCommand(request.AccountNumber, request.InitialBalance, CallerEmail), ct);

        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>Credits an amount to an account.</summary>
    [HttpPost("{id:guid}/deposit")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deposit(
        Guid id, [FromBody] AmountRequest request, CancellationToken ct)
    {
        var newBalance = await _mediator.Send(
            new DepositCommand(id, request.Amount, CallerEmail), ct);
        return Ok(new BalanceResponse(id, newBalance));
    }

    /// <summary>Debits an amount from an account.</summary>
    [HttpPost("{id:guid}/withdraw")]
    [ProducesResponseType(typeof(BalanceResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Withdraw(
        Guid id, [FromBody] AmountRequest request, CancellationToken ct)
    {
        var newBalance = await _mediator.Send(
            new WithdrawCommand(id, request.Amount, CallerEmail), ct);
        return Ok(new BalanceResponse(id, newBalance));
    }
}

// ── Request / Response models ─────────────────────────────────────────────────

public record CreateAccountRequest(string AccountNumber, decimal InitialBalance);
public record AmountRequest(decimal Amount);
public record BalanceResponse(Guid AccountId, decimal NewBalance);
