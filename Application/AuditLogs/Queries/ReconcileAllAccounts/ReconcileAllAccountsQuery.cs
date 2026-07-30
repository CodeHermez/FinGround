using MediatR;

namespace FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;

public record ReconcileAllAccountsQuery : IRequest<ReconciliationSweepDto>;
