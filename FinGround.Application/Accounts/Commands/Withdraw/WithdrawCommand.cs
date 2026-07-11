using MediatR;

namespace FinGround.Application.Accounts.Commands.Withdraw;

public record WithdrawCommand(
    Guid AccountId,
    decimal Amount,
    string? InitiatedBy = null
) : IRequest<decimal>;
