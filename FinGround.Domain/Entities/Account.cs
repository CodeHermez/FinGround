namespace FinGround.Domain.Entities;

public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string AccountNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }

    public ICollection<Transaction> OutgoingTransactions { get; set; } = new List<Transaction>();
    public ICollection<Transaction> IncomingTransactions { get; set; } = new List<Transaction>();
}
