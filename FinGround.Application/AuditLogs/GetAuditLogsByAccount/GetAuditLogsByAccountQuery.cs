using FinGround.Application.Common.Models;
using MediatR;

namespace FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;

// returns a paginated audit trail for a single account, newest first.
// filters all optional, combinable:
//   Command — case-insensitive substring match ("Deposit", "Transfer", etc.)
//   From    — entries on or after this UTC timestamp
//   To      — entries on or before this UTC timestamp
// pagination defaults: Page = 1, PageSize = 50, maximum PageSize = 100.
public record GetAuditLogsByAccountQuery(
    Guid AccountId,
    string? Command = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<AuditLogDto>>;
