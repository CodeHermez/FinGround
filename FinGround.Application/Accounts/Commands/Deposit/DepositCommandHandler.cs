using FinGround.Application.Common.Exceptions;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.Accounts.Commands.Deposit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.Accounts.Commands.Deposit;

public class DepositCommandHandler : IRequestHandler<DepositCommand, decimal>
{
    private readonly IBankDbContext _context;

    public DepositCommandHandler(IBankDbContext context) => _context = context;

    public async Task<decimal> Handle(DepositCommand request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
            throw new ArgumentException("Deposit amount must be greater than zero.");

        var account = await _context.Accounts
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var balanceBefore = account.Balance;
        account.Balance += request.Amount;

        _context.Accounts.Update(account);

        _context.AuditLogs.Add(AuditLog.Create(
            command: "Deposit",
            accountId: account.Id,
            balanceBefore: balanceBefore,
            balanceAfter: account.Balance,
            amount: request.Amount,
            initiatedBy: request.InitiatedBy));

        await _context.SaveChangesAsync(cancellationToken);

        return account.Balance;
    }
}
