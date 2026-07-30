using FinGround.Application.Common.Exceptions;
using FinGround.Application.Common.Models;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;
using FinGround.Application.Common.Exceptions;
using FinGround.Application.Common.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;

public class GetAuditLogsByAccountQueryHandler
    : IRequestHandler<GetAuditLogsByAccountQuery, PagedResult<AuditLogDto>>
{
    private const int MaxPageSize = 100;

    private readonly IBankDbContext _context;

    public GetAuditLogsByAccountQueryHandler(IBankDbContext context) =>
        _context = context;

    public async Task<PagedResult<AuditLogDto>> Handle(
        GetAuditLogsByAccountQuery request, CancellationToken cancellationToken)
    {
        var accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == request.AccountId, cancellationToken);

        if (!accountExists)
            throw new NotFoundException(nameof(Account), request.AccountId);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        //build filtered query
        var query = _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.AccountId == request.AccountId);

        if (!string.IsNullOrWhiteSpace(request.Command))
        {
            var needle = request.Command.Trim().ToLower();
            query = query.Where(l => l.Command.ToLower().Contains(needle));
        }

        if (request.From.HasValue)
            query = query.Where(l => l.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(l => l.Timestamp <= request.To.Value);

        // count then page
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
