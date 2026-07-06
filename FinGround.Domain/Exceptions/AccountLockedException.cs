namespace FinGroud.Domain.Exceptions;
// thrown when a login attempt is made against an account that has been
// temporarily locked after too many consecutive failed attempts.
public class AccountLockedException : Exception
{
    public DateTimeOffset LockedUntil { get; }
    public AccountLockedException(DateTimeOffset lockedUntil)
       : base($"Account temporarily locked due to too many failed login attempts. "+$"Try again after {lockedUntil:R}.")
    {
        LockedUntil = lockedUntil;
    }
}
