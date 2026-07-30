using FinGround.Application.Common.Exceptions;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.Accounts.Commands.CreateAccount;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.Accounts.Commands.CreateAccount;

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand, Guid>
{
    private readonly IBankDbContext _context;

    public CreateAccountCommandHandler(IBankDbContext context) => _context = context;

    public async Task<Guid> Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        if (request.InitialBalance < 0)
            throw new ArgumentException("Initial balance cannot be negative.");

        var exists = await _context.Accounts
            .AnyAsync(a => a.AccountNumber == request.AccountNumber, cancellationToken);

        if (exists)
            throw new DuplicateAccountNumberException(request.AccountNumber);

        var account = new Account
        {
            AccountNumber = request.AccountNumber,
            Balance = request.InitialBalance
        };

        _context.Accounts.Add(account);

        _context.AuditLogs.Add(AuditLog.Create(
            command: "CreateAccount",
            accountId: account.Id,
            balanceBefore: 0m,
            balanceAfter: account.Balance,
            amount: account.Balance,
            initiatedBy: request.InitiatedBy,
            notes: $"Account {account.AccountNumber} opened"));

        await _context.SaveChangesAsync(cancellationToken);

        return account.Id;
    }
}
