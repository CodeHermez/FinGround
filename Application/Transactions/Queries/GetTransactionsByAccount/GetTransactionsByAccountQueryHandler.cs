using FinGround.Application.Common.Exceptions;
using FinGround.Application.Common.Models;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.Common.Exceptions;
using FinGround.Application.Common.Models;
using FinGround.Application.Interfaces;
using FinGround.Application.Transactions.Queries.GetTransactionsByAccount;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.Transactions.Queries.GetTransactionsByAccount;

public class GetTransactionsByAccountQueryHandler
    : IRequestHandler<GetTransactionsByAccountQuery, PagedResult<TransactionDto>>
{
    private const int MaxPageSize = 100;

    private readonly IBankDbContext _context;

    public GetTransactionsByAccountQueryHandler(IBankDbContext context) =>
        _context = context;

    public async Task<PagedResult<TransactionDto>> Handle(
        GetTransactionsByAccountQuery request, CancellationToken cancellationToken)
    {
        var accountExists = await _context.Accounts
            .AnyAsync(a => a.Id == request.AccountId, cancellationToken);

        if (!accountExists)
            throw new NotFoundException(nameof(Account), request.AccountId);

        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        //Build filtered query
        var query = _context.Transactions
            .AsNoTracking()
            .Where(t => t.SourceAccountId == request.AccountId ||
                        t.DestinationAccountId == request.AccountId);

        if (request.MinAmount.HasValue)
            query = query.Where(t => t.Amount >= request.MinAmount.Value);

        if (request.MaxAmount.HasValue)
            query = query.Where(t => t.Amount <= request.MaxAmount.Value);

        if (request.From.HasValue)
            query = query.Where(t => t.Timestamp >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => t.Timestamp <= request.To.Value);

        //Count then page
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.Timestamp)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TransactionDto(
                t.Id,
                t.SourceAccountId,
                t.DestinationAccountId,
                t.Amount,
                t.Timestamp))
            .ToListAsync(cancellationToken);

        return new PagedResult<TransactionDto>(items, totalCount, page, pageSize);
    }
}
