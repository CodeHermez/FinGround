using MediatR;

namespace FinGround.Application.Accounts.Commands.CreateAccount;

public record CreateAccountCommand(
    string AccountNumber,
    decimal InitialBalance,
    string? InitiatedBy = null
) : IRequest<Guid>;
