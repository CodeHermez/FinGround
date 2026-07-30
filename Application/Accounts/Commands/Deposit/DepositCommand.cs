using MediatR;

namespace FinGround.Application.Accounts.Commands.Deposit;

public record DepositCommand(
    Guid AccountId,
    decimal Amount,
    string? InitiatedBy = null
) : IRequest<decimal>;
