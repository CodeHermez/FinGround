using FinGround.Application.Auth.Common;
using FinGround.Application.Common.Interfaces;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.Auth.Commands.Register;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Auth.Commands.Register;

public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResultDto>
{
    private readonly IBankDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ITokenService _tokenService;

    public RegisterCommandHandler(
        IBankDbContext context,
        IPasswordHasher hasher,
        ITokenService tokenService)
    {
        _context = context;
        _hasher = hasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResultDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new ArgumentException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            throw new ArgumentException("Password must be at least 8 characters.");

        var emailNormalized = request.Email.Trim().ToLowerInvariant();

        var exists = await _context.Users
            .AnyAsync(u => u.Email == emailNormalized, cancellationToken);

        if (exists)
            throw new InvalidOperationException($"An account with email '{emailNormalized}' already exists.");

        var user = new User
        {
            Email = emailNormalized,
            PasswordHash = _hasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        return new AuthResultDto(
            user.Id,
            user.Email,
            user.FullName,
            _tokenService.GenerateToken(user),
            _tokenService.ExpiresAt());
    }
}
