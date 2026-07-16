namespace FinGround.Application.Common.Exceptions;

public class InsufficientFundsException : InvalidOperationException
{
    public InsufficientFundsException(string accountNumber, decimal balance, decimal requested)
        : base($"Account '{accountNumber}' has insufficient funds. " +
               $"Available: {balance:C}, Requested: {requested:C}")
    { }
}
