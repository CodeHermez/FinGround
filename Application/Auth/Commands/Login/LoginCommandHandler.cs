using FinGround.Application.Auth.Common;
using FinGround.Application.Common.Interfaces;
using FinGround.Application.Interfaces;
using FinGround.Domain.Exceptions;
using FinGround.Application.Auth.Commands.Login;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Auth.Commands.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResultDto>
{
    // lockout policy constants — centralised here so they are easy to find
    // and could be moved to IOptions in a production system.
    private const int MaxFailedAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    private readonly IBankDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public LoginCommandHandler(
        IBankDbContext context,
        IPasswordHasher hasher,
        ITokenService tokenService)
    {
        _context = context;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResultDto> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailNormalized = request.Email.Trim().ToLowerInvariant();
        var now = DateTimeOffset.UtcNow;

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == emailNormalized, cancellationToken);

        // Unified message never reveal whether the email exists.
        const string InvalidCredentials = "Invalid email or password.";

        if (user is null)
            throw new UnauthorizedAccessException(InvalidCredentials);

        // Lockout check 
        if (user.IsLocked(now))
            throw new AccountLockedException(user.LockedUntil!.Value);

        // Password verification 
        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedAttempt(now, MaxFailedAttempts, LockoutDuration);
            await _context.SaveChangesAsync(cancellationToken);
            throw new UnauthorizedAccessException(InvalidCredentials);
        }

        //Successful login clear the counter 
        user.ResetLockout();
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.Id,
            user.Email,
            user.FullName,
            _tokenService.GenerateToken(user),
            _tokenService.ExpiresAt());
    }
}
