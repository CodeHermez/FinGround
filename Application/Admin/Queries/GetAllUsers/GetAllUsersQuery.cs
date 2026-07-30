using FinGround.Application.Common.Models;
using MediatR;

namespace FinGround.Application.Admin.Queries.GetAllUsers;

/// <summary>
/// Returns a paginated, optionally-filtered list of users.
///
/// Filters (all optional, combinable):
///   Role     — exact match, case-insensitive ("Admin" | "User")
///   IsLocked — true = only locked accounts, false = only unlocked accounts
///
/// Pagination defaults: Page = 1, PageSize = 20, maximum PageSize = 100.
/// </summary>
public record GetAllUsersQuery(
    string? Role = null,
    bool? IsLocked = null,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<UserSummaryDto>>;
