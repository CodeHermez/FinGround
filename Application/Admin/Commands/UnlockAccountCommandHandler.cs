using FinGround.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Admin.Commands.UnlockAccount;

public class UnlockAccountCommandHandler : IRequestHandler<UnlockAccountCommand>
{
    private readonly IBankDbContext _context;

    public UnlockAccountCommandHandler(IBankDbContext context) =>
        _context = context;

    public async Task Handle(UnlockAccountCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        user.ResetLockout();
        await _context.SaveChangesAsync(cancellationToken);
    }
}
