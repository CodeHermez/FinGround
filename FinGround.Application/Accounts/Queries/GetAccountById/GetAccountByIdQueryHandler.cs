using FinGround.Application.Common.Exceptions;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.Accounts.Queries.GetAccountById;

public class GetAccountByIdQueryHandler : IRequestHandler<GetAccountByIdQuery, AccountDto>
{
    private readonly IBankDbContext _context;

    public GetAccountByIdQueryHandler(IBankDbContext context) => _context = context;

    public async Task<AccountDto> Handle(GetAccountByIdQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        return new AccountDto(account.Id, account.AccountNumber, account.Balance);
    }
}
