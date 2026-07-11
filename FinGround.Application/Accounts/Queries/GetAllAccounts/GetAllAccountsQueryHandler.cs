using FinGround.Application.Accounts.Queries.GetAccountById;
using FinGround.Application.Common.Models;
using FinGround.Application.Interfaces;
using FinGround.Application.Accounts.Queries.GetAccountById;
using FinGround.Application.Accounts.Queries.GetAllAccounts;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace FinGround.Application.Accounts.Queries.GetAllAccounts;

public class GetAllAccountsQueryHandler
    : IRequestHandler<GetAllAccountsQuery, PagedResult<AccountDto>>
{
    private const int MaxPageSize = 100;

    private readonly IBankDbContext _context;

    public GetAllAccountsQueryHandler(IBankDbContext context) =>
        _context = context;

    public async Task<PagedResult<AccountDto>> Handle(
        GetAllAccountsQuery request, CancellationToken cancellationToken)
    {
        // validate paging inputs
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, MaxPageSize);

        // build filtered base query (all predicates evaluated in the DB)
        var query = _context.Accounts.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.AccountNumber))
        {
            var needle = request.AccountNumber.Trim().ToLower();
            query = query.Where(a => a.AccountNumber.ToLower().Contains(needle));
        }

        if (request.MinBalance.HasValue)
            query = query.Where(a => a.Balance >= request.MinBalance.Value);

        if (request.MaxBalance.HasValue)
            query = query.Where(a => a.Balance <= request.MaxBalance.Value);

        // count filtered results before paging
        var totalCount = await query.CountAsync(cancellationToken);

        // Fetch the requested page, sorted consistently by AccountNumber
        var items = await query
            .OrderBy(a => a.AccountNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(a => new AccountDto(a.Id, a.AccountNumber, a.Balance))
            .ToListAsync(cancellationToken);

        return new PagedResult<AccountDto>(items, totalCount, page, pageSize);
    }
}
