using FinGround.Application.Interfaces;
using FinGround.Application.Admin.Queries.GetUserDetail;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Admin.Queries.GetUserDetail;

public class GetUserDetailQueryHandler
    : IRequestHandler<GetUserDetailQuery, UserDetailDto>
{
    private readonly IBankDbContext _context;

    public GetUserDetailQueryHandler(IBankDbContext context) =>
        _context = context;

    public async Task<UserDetailDto> Handle(
        GetUserDetailQuery request, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException($"User {request.UserId} not found.");

        // audit logs are attributed by email (the InitiatedBy claim written at
        // command time).  A single query fetches all entries across every account
        // this user has ever touched, newest first.
        var auditTrail = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.InitiatedBy == user.Email)
            .OrderByDescending(l => l.Timestamp)
            .Select(l => new AuditEntryDto(
                l.Id,
                l.AccountId,
                l.Command,
                l.BalanceBefore,
                l.BalanceAfter,
                l.Amount,
                l.Notes,
                l.Timestamp))
            .ToListAsync(cancellationToken);

        return new UserDetailDto(
            user.Id,
            user.Email,
            user.FullName,
            user.Role,
            user.CreatedAt,
            user.FailedLoginAttempts,
            user.LockedUntil,
            user.IsLocked(now),
            auditTrail);
    }
}
