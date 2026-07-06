namespace FinGround.Domain.Entities;

public class AuditLog
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Command { get; private set; } = "";
    public Guid AccountId { get; private set; }
    public decimal BalanceBefore { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public decimal Amount { get; private set; }
    public string? InitiatedBy { get; private set; }
    public string? Notes { get; private set; }

    private AuditLog() { }

    public static AuditLog Create(
        string command,
        Guid accountId,
        decimal balanceBefore,
        decimal balanceAfter,
        decimal amount,
        string? initiatedBy = null,
        string? notes = null)
    {
        return new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Command = command,
            AccountId = accountId,
            BalanceBefore = balanceBefore,
            BalanceAfter = balanceAfter,
            Amount = amount,
            InitiatedBy = initiatedBy,
            Notes = notes
        };
    }
}
