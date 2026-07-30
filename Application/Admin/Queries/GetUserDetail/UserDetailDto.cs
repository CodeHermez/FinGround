namespace FinGround.Application.Admin.Queries.GetUserDetail;

/// Single audit-trail entry projected for the admin detail view.
public record AuditEntryDto(
    Guid EntryId,
    Guid AccountId,
    string Command,
    decimal BalanceBefore,
    decimal BalanceAfter,
    decimal Amount,
    string? Notes,
    DateTime Timestamp
);

/// full user profile plus their complete audit history, returned by
/// GetUserDetailQuery.  Password hashes are never included.
public record UserDetailDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime CreatedAt,
    int FailedLoginAttempts,
    DateTimeOffset? LockedUntil,
    bool IsLocked,
    IReadOnlyList<AuditEntryDto> AuditTrail
);
