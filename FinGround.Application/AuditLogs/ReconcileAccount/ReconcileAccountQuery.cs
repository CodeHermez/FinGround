using MediatR;

namespace FinGround.Application.AuditLogs.Queries.ReconcileAccount;

public record ReconcileAccountQuery(Guid AccountId) : IRequest<ReconciliationReportDto>;
