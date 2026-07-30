namespace FinGround.Application.Common.Exceptions;

public class DuplicateAccountNumberException : InvalidOperationException
{
    public DuplicateAccountNumberException(string accountNumber)
        : base($"An account with number '{accountNumber}' already exists.") { }
}
