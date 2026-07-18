using MediatR;

namespace FinGround.Application.Transactions.Commands.TransferFunds;

public record TransferFundsCommand(
    Guid SourceAccountId,
    Guid DestinationAccountId,
    decimal Amount,
    string? InitiatedBy = null
) : IRequest<Guid>;
