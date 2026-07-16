using FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;
using FinGround.Application.Common.Models;
using MediatR;

namespace FinGround.Application.AuditLogs.Queries.GetAllAuditLogs;

// returns a paginated, optionally-filtered list of all audit log entries
// across every account, newest first
// filters all optional, combinable:
//   AccountId   — restrict to one account
//   Command     — case-insensitive substring match ("Deposit", "Transfer", etc.)
//   InitiatedBy — case-insensitive substring match on the email/identifier
//   From        — entries on or after this UTC timestamp
//   To          — entries on or before this UTC 
// pagination defaults: Page = 1, PageSize = 50, maximum PageSize = 100.

public record GetAllAuditLogsQuery(
    Guid? AccountId = null,
    string? Command = null,
    string? InitiatedBy = null,
    DateTime? From = null,
    DateTime? To = null,
    int Page = 1,
    int PageSize = 50
) : IRequest<PagedResult<AuditLogDto>>;
