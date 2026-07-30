using FinGround.Application.Common.Models;
using MediatR;

namespace FinGround.Application.Transactions.Queries.GetTransactionsByAccount;

/// <summary>
/// Returns a paginated list of transactions where the given account is either
/// the source or destination.
///
/// Filters (all optional, combinable):
///   MinAmount — transactions with Amount >= this value
///   MaxAmount — transactions with Amount &lt;= this value
///   From      — transactions on or after this UTC timestamp
///   To        — transactions on or before this UTC timestamp
///
/// Pagination defaults: Page = 1, PageSize = 20, maximum PageSize = 100.
/// Results are ordered newest-first.
/// </summary>
public record GetTransactionsByAccountQuery(
    Guid AccountId,
    decimal? MinAmount = null,
    decimal? MaxAmount = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<TransactionDto>>;
