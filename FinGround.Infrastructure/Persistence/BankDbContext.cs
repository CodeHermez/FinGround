using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;
using System.Security.Principal;
using System.Transactions;

namespace FinGround.Infrastructure.Persistence;

public class BankDbContext : DbContext, IBankDbContext
{
    public BankDbContext(DbContextOptions<BankDbContext> options) : base(options) { }

    public DbSet<Account> Accounts => Set<Account>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<User> Users => Set<User>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Account>(entity =>
        {
            entity.HasKey(a => a.Id);

            entity.Property(a => a.AccountNumber)
                  .IsRequired()
                  .HasMaxLength(20);

            entity.HasIndex(a => a.AccountNumber)
                  .IsUnique();

            entity.Property(a => a.Balance)
                  .HasColumnType("numeric(18,2)");
        });

        modelBuilder.Entity<Transaction>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Amount)
                  .HasColumnType("numeric(18,2)");

            entity.Property(t => t.Timestamp)
                  .HasDefaultValueSql("NOW()");

            entity.HasOne(t => t.SourceAccount)
                  .WithMany(a => a.OutgoingTransactions)
                  .HasForeignKey(t => t.SourceAccountId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(t => t.DestinationAccount)
                  .WithMany(a => a.IncomingTransactions)
                  .HasForeignKey(t => t.DestinationAccountId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.HasIndex(u => u.Email)
                  .IsUnique();

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.FullName)
                  .IsRequired()
                  .HasMaxLength(200);

            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasDefaultValue("User");

            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("NOW()");

            entity.Property(u => u.FailedLoginAttempts)
                  .HasDefaultValue(0);

            entity.Property(u => u.LockedUntil)
                  .IsRequired(false);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(l => l.Id);

            entity.Property(l => l.Command)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(l => l.BalanceBefore)
                  .HasColumnType("numeric(18,2)");

            entity.Property(l => l.BalanceAfter)
                  .HasColumnType("numeric(18,2)");

            entity.Property(l => l.Amount)
                  .HasColumnType("numeric(18,2)");

            entity.Property(l => l.InitiatedBy)
                  .HasMaxLength(256);

            entity.Property(l => l.Timestamp)
                  .HasDefaultValueSql("NOW()");

            entity.HasIndex(l => l.AccountId);
            entity.HasIndex(l => l.Timestamp);
        });
    }
}
