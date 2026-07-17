using FinGround.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using System.Transactions;

namespace FinGround.Application.Interfaces;

public interface IBankDbContext
{
    DbSet<Account> Accounts { get; }
    DbSet<Transaction> Transactions { get; }
    DbSet<User> Users { get; }
    DbSet<AuditLog> AuditLogs { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
