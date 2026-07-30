namespace FinGround.Application.Accounts.Queries.GetAccountById;

public record AccountDto(
    Guid Id,
    string AccountNumber,
    decimal Balance
);
