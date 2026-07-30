using FinGround.Application.Common.Models;
using FinGround.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Admin.Queries.GetAllUsers;

public class GetAllUsersQueryHandler
    : IRequestHandler<GetAllUsersQuery, PagedResult<UserSummaryDto>>
{
    private const int MaxPageSize = 100;

    private readonly IBankDbContext _context;

    public GetAllUsersQueryHandler(IBankDbContext context) =>
        _context = context;

    public async Task<PagedResult<UserSummaryDto>> Handle(
        GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        // clamp / validate paging inputs
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);
        var now = DateTimeOffset.UtcNow;

        // build filtered base query (evaluated in the DB)
        var query = _context.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Role))
            query = query.Where(u =>
                u.Role.ToLower() == request.Role.Trim().ToLower());

        if (request.IsLocked.HasValue)
        {
            query = request.IsLocked.Value
                ? query.Where(u => u.LockedUntil != null && u.LockedUntil.Value > now)
                : query.Where(u => u.LockedUntil == null || u.LockedUntil.Value <= now);
        }

        // count before paging — two sequential queries on the same context
        // (EF Core DbContext is not thread-safe, so Task.WhenAll is not used here)
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderBy(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserSummaryDto(
                u.Id,
                u.Email,
                u.FullName,
                u.Role,
                u.CreatedAt,
                u.FailedLoginAttempts,
                u.LockedUntil,
                u.LockedUntil != null && u.LockedUntil.Value > now))
            .ToListAsync(cancellationToken);

        return new PagedResult<UserSummaryDto>(items, totalCount, page, pageSize);
    }
}
