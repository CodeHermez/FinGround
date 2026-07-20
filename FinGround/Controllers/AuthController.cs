using FinGround.Application.Auth.Commands.Login;
using FinGround.Application.Auth.Commands.Register;
using FinGround.Application.Auth.Common;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace FinGround.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator) => _mediator = mediator;

    /// <summary>
    /// Creates a new user account and returns a signed JWT token.
    /// The token can be used immediately to call all protected endpoints.
    ///
    /// Rate limit: 3 registrations per 10 minutes per IP address.
    /// Exceeding this returns 429 Too Many Requests with a Retry-After header.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting("auth-register")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new RegisterCommand(request.Email, request.Password, request.FullName), ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// Authenticates an existing user and returns a fresh signed JWT token.
    ///
    /// Rate limit: 5 attempts per 60 seconds per IP address (sliding window).
    /// The sliding window refills gradually — quota does not fully reset all at
    /// once — making burst-and-wait brute-force attacks impractical.
    /// Exceeding this returns 429 Too Many Requests with a Retry-After header.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting("auth-login")]
    [ProducesResponseType(typeof(AuthResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(
            new LoginCommand(request.Email, request.Password), ct);

        return Ok(result);
    }
}

// ── Request models ────────────────────────────────────────────────────────────

public record RegisterRequest(string Email, string Password, string FullName);
public record LoginRequest(string Email, string Password);
