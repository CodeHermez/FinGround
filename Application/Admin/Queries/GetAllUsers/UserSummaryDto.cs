namespace FinGround.Application.Admin.Queries.GetAllUsers;
/// projection returned by GetAllUsersQuery.
/// doesnt includes password hashes or raw secrets.
public record UserSummaryDto(
    Guid UserId,
    string Email,
    string FullName,
    string Role,
    DateTime CreatedAt,
    int FailedLoginAttempts,
    DateTimeOffset? LockedUntil,
    bool IsLocked
);
