using FinGround.Application.Common.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Principal;
using System.Transactions;

namespace FinGround.Infrastructure.Persistence.Seeders;

public class DatabaseSeeder
{
    private readonly BankDbContext _context;
    private readonly IPasswordHasher _hasher;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        BankDbContext context,
        IPasswordHasher hasher,
        ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _hasher = hasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        await _context.Database.MigrateAsync(ct);
        // only seed when the database is completely empty
        if (await _context.Users.AnyAsync(ct))
        {
            _logger.LogInformation("Database already has data — skipping seed.");
            return;
        }

        _logger.LogInformation("Seeding demo data…");

        // demo user (admin role)
        var demoUser = new User
        {
            Email = "demo@banking-sandbox.dev",
            PasswordHash = _hasher.Hash("Demo1234!"),
            FullName = "Demo User",
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(demoUser);

        // Two pre-funded accounts
        var checking = new Account
        {
            AccountNumber = "CHK-000001",
            Balance = 5_000.00m
        };

        var savings = new Account
        {
            AccountNumber = "SAV-000001",
            Balance = 12_500.00m
        };

        _context.Accounts.Add(checking);
        _context.Accounts.Add(savings);

        // an initial seed transaction (checking → savings)
        // persisted after both accounts get their IDs assigned by EF
        await _context.SaveChangesAsync(ct);

        var seedTx = new Transaction
        {
            SourceAccountId = checking.Id,
            DestinationAccountId = savings.Id,
            Amount = 2_500.00m,
            Timestamp = DateTime.UtcNow.AddDays(-7)
        };

        _context.Transactions.Add(seedTx);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Seed complete. Demo login → email: {Email}  password: Demo1234!  role: Admin",
            demoUser.Email);
    }
}
