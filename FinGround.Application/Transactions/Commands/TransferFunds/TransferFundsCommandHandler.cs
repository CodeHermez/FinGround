using FinGround.Application.Common.Exceptions;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.Common.Exceptions;
using FinGround.Application.Interfaces;
using FinGround.Application.Transactions.Commands.TransferFunds;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using System.Transactions;

namespace FinGround.Application.Transactions.Commands.TransferFunds;

public class TransferFundsCommandHandler : IRequestHandler<TransferFundsCommand, Guid>
{
    private readonly IBankDbContext _context;

    public TransferFundsCommandHandler(IBankDbContext context) => _context = context;

    public async Task<Guid> Handle(TransferFundsCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Transfer amount must be greater than zero.");

        if (request.SourceAccountId == request.DestinationAccountId)
            throw new ArgumentException("Source and destination accounts must be different.");

        var source = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.SourceAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.SourceAccountId);

        var destination = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.DestinationAccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.DestinationAccountId);

        if (source.Balance < request.Amount)
            throw new InsufficientFundsException(source.AccountNumber, source.Balance, request.Amount);

        var srcBefore = source.Balance;
        var destBefore = destination.Balance;

        source.Balance -= request.Amount;
        destination.Balance += request.Amount;

        var transaction = new Transaction
        {
            SourceAccountId = source.Id,
            DestinationAccountId = destination.Id,
            Amount = request.Amount,
            Timestamp = DateTime.UtcNow
        };

        _context.Accounts.Update(source);
        _context.Accounts.Update(destination);
        _context.Transactions.Add(transaction);

        _context.AuditLogs.Add(AuditLog.Create(
            command: "TransferFunds:Debit",
            accountId: source.Id,
            balanceBefore: srcBefore,
            balanceAfter: source.Balance,
            amount: request.Amount,
            initiatedBy: request.InitiatedBy,
            notes: $"Transfer to {destination.AccountNumber} (txn {transaction.Id})"));

        _context.AuditLogs.Add(AuditLog.Create(
            command: "TransferFunds:Credit",
            accountId: destination.Id,
            balanceBefore: destBefore,
            balanceAfter: destination.Balance,
            amount: request.Amount,
            initiatedBy: request.InitiatedBy,
            notes: $"Transfer from {source.AccountNumber} (txn {transaction.Id})"));

        await _context.SaveChangesAsync(cancellationToken);

        return transaction.Id;
    }
}
