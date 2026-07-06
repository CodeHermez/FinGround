namespace FinGroud.Domain.Entities;

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid SourceAccountId { get; set; }
    public Guid DestinationAccountId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public Account SourceAccount { get; set; } = null!;
    public Account DestinationAccount { get; set; } = null!;
}
