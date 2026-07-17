using .Application.AuditLogs.Queries.ReconcileAllAccounts;
using FinGround.Application.AuditLogs.Queries.ReconcileAccount;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.AuditLogs.Queries.ReconcileAccount;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;

public class ReconcileAllAccountsQueryHandler
    : IRequestHandler<ReconcileAllAccountsQuery, ReconciliationSweepDto>
{
    private readonly IBankDbContext _context;

    public ReconcileAllAccountsQueryHandler(IBankDbContext context) => _context = context;

    public async Task<ReconciliationSweepDto> Handle(
        ReconcileAllAccountsQuery request, CancellationToken cancellationToken)
    {
        var accounts = await _context.Accounts
            .AsNoTracking()
            .OrderBy(a => a.AccountNumber)
            .ToListAsync(cancellationToken);

        // Load all audit logs in a single round-trip, pre-grouped by account.
        var allLogs = await _context.AuditLogs
            .AsNoTracking()
            .OrderBy(l => l.AccountId)
            .ThenBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);

        var logsByAccount = allLogs
            .GroupBy(l => l.AccountId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var results = new List<AccountReconciliationDto>(accounts.Count);

        foreach (var account in accounts)
        {
            logsByAccount.TryGetValue(account.Id, out var entries);

            results.Add(Reconcile(account, entries ?? new List<AuditLog>()));
        }

        var reconciled = results.Count(r => r.Status == "Reconciled");
        var discrepancies = results.Count(r => r.Status == "Discrepancy");
        var gapsDetected = results.Count(r => r.Status == "TrailGapDetected");
        var noAuditTrail = results.Count(r => r.Status == "NoAuditTrail");

        return new ReconciliationSweepDto(
            RunAt: DateTime.UtcNow,
            TotalAccounts: accounts.Count,
            Reconciled: reconciled,
            Discrepancies: discrepancies,
            TrailGapsDetected: gapsDetected,
            NoAuditTrail: noAuditTrail,
            AllClean: reconciled == accounts.Count,
            Accounts: results);
    }

    // ── Pure reconciliation logic (mirrors ReconcileAccountQueryHandler) ───────

    private static AccountReconciliationDto Reconcile(
        Account account, List<AuditLog> entries)
    {
        if (entries.Count == 0)
        {
            return new AccountReconciliationDto(
                AccountId: account.Id,
                AccountNumber: account.AccountNumber,
                StoredBalance: account.Balance,
                ComputedBalance: null,
                Discrepancy: null,
                Status: "NoAuditTrail",
                EntryCount: 0,
                AuditTrailOpeningBalance: null,
                Gaps: Array.Empty<TrailGapDto>());
        }

        var gaps = new List<TrailGapDto>();

        for (int i = 1; i < entries.Count; i++)
        {
            var expected = entries[i - 1].BalanceAfter;
            var actual = entries[i].BalanceBefore;

            if (expected != actual)
            {
                gaps.Add(new TrailGapDto(
                    EntryId: entries[i].Id,
                    EntryIndex: i,
                    ExpectedBalanceBefore: expected,
                    ActualBalanceBefore: actual,
                    Variance: actual - expected));
            }
        }

        var computedBalance = entries[^1].BalanceAfter;
        var discrepancy = account.Balance - computedBalance;

        var status = gaps.Count > 0 ? "TrailGapDetected"
                   : discrepancy != 0m ? "Discrepancy"
                   : "Reconciled";

        return new AccountReconciliationDto(
            AccountId: account.Id,
            AccountNumber: account.AccountNumber,
            StoredBalance: account.Balance,
            ComputedBalance: computedBalance,
            Discrepancy: discrepancy,
            Status: status,
            EntryCount: entries.Count,
            AuditTrailOpeningBalance: entries[0].BalanceBefore,
            Gaps: gaps);
    }
}
