using FinGround.Application.Accounts.Queries.GetAccountById;
using FinGround.Application.Common.Models;
using FinGround.Application.Accounts.Queries.GetAccountById;
using MediatR;

namespace FinGround.Application.Accounts.Queries.GetAllAccounts;

/// Returns a paginated, optionally-filtered list of accounts.
///
/// Filters options
///   AccountNumber — case-insensitive substring match ("CHK", "000", etc.)
///   MinBalance    — accounts with Balance >= this value
///   MaxBalance    — accounts with Balance &lt;= this value
///
/// Pagination defaults: Page = 1, PageSize = 20, maximum PageSize = 100.
public record GetAllAccountsQuery(
    string? AccountNumber = null,
    decimal? MinBalance = null,
    decimal? MaxBalance = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<AccountDto>>;
