using FinGround.Application.Common.Exceptions;
using FinGround.Application.Interfaces;
using FinGround.Domain.Entities;
using FinGround.Application.AuditLogs.Queries.ReconcileAccount;
using FinGround.Application.Common.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace FinGround.Application.AuditLogs.Queries.ReconcileAccount;

public class ReconcileAccountQueryHandler
    : IRequestHandler<ReconcileAccountQuery, ReconciliationReportDto>
{
    private readonly IBankDbContext _context;

    public ReconcileAccountQueryHandler(IBankDbContext context) => _context = context;

    public async Task<ReconciliationReportDto> Handle(
        ReconcileAccountQuery request, CancellationToken cancellationToken)
    {
        var account = await _context.Accounts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == request.AccountId, cancellationToken)
            ?? throw new NotFoundException(nameof(Account), request.AccountId);

        var entries = await _context.AuditLogs
            .AsNoTracking()
            .Where(l => l.AccountId == request.AccountId)
            .OrderBy(l => l.Timestamp)
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            return new ReconciliationReportDto(
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

        // ── Detect chain gaps ───────────────────────────────────────────────
        // Each entry's BalanceBefore must equal the previous entry's BalanceAfter.
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

        // ── Compute expected current balance ────────────────────────────────
        // Regardless of gaps the best "replay" value is the last entry's BalanceAfter.
        var computedBalance = entries[^1].BalanceAfter;
        var discrepancy = account.Balance - computedBalance;

        string status;
        if (gaps.Count > 0)
            status = "TrailGapDetected";
        else if (discrepancy != 0m)
            status = "Discrepancy";
        else
            status = "Reconciled";

        return new ReconciliationReportDto(
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
