using FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;
using FinGround.Application.Common.Models;
using FinGround.Application.Interfaces;
using FinGround.Application.AuditLogs.Queries.GetAllAuditLogs;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.AuditLogs.Queries.GetAllAuditLogs;

public class GetAllAuditLogsQueryHandler
    : IRequestHandler<GetAllAuditLogsQuery, PagedResult<AuditLogDto>>
{
    private const int MaxPageSize = 100;

    private readonly IBankDbContext _context;

    public GetAllAuditLogsQueryHandler(IBankDbContext context) =>
        _context = context;

    public async Task<PagedResult<AuditLogDto>> Handle(
        GetAllAuditLogsQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        // Build filtered query
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (request.AccountId.HasValue)
            query = query.Where(l => l.AccountId == request.AccountId.Value);

        if (!string.IsNullOrWhiteSpace(request.Command))
        {
            var needle = request.Command.Trim().ToLower();
            query = query.Where(l => l.Command.ToLower().Contains(needle));
        }

        if (!string.IsNullOrWhiteSpace(request.InitiatedBy))
        {
            var needle = request.InitiatedBy.Trim().ToLower();
            query = query.Where(l => l.InitiatedBy != null &&
                                     l.InitiatedBy.ToLower().Contains(needle));
        }

        if (request.From.HasValue)
            query = query.Where(l => l.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.Timestamp <= request.To.Value);

        // Count then page
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(l => l.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogDto(
                l.Id,
                l.Timestamp,
                l.Command,
                l.AccountId,
                l.BalanceBefore,
                l.BalanceAfter,
                l.Amount,
                l.InitiatedBy,
                l.Notes))
            .ToListAsync(cancellationToken);

        return new PagedResult<AuditLogDto>(items, totalCount, page, pageSize);
    }
}
