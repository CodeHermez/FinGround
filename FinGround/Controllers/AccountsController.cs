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

    //returns a paginated, optionally-filtered list of accounts.
    // all filter parameters are optional and combinable:
    // accountNumber: case-insensitive substring match ("CHK", "SAV", "0001", etc.)
    // minBalance: only accounts with Balance >= this value
    //maxBalance: only accounts with Balance &lt;= this value
    //
    //pagination defaults: page = 1, pageSize = 20, maximum pageSize = 100
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

    ///returns a single account by ID.
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(AccountDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAccountByIdQuery(id), ct);
        return Ok(result);
    }

    //opens a new bank account.
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

    //credits an amount to an account.
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

    //debits an amount from an account.
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

// request / response models

public record CreateAccountRequest(string AccountNumber, decimal InitialBalance);
public record AmountRequest(decimal Amount);
public record BalanceResponse(Guid AccountId, decimal NewBalance);
