using MediatR;

namespace FinGround.Application.Admin.Commands.UnlockAccount;

/// <summary>
/// Clears the lockout on a user account, resetting failed-attempt counter
/// and removing the LockedUntil timestamp.  Requires the Admin role.
/// </summary>
public record UnlockAccountCommand(Guid UserId) : IRequest;
