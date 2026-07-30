using MediatR;

namespace FinGround.Application.Accounts.Queries.GetAccountById;

public record GetAccountByIdQuery(Guid AccountId) : IRequest<AccountDto>;
