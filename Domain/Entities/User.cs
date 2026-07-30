namespace FinGround.Domain.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "User";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    //Lockout tracking 
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTimeOffset? LockedUntil { get; set; } = null;

    // Convenience helpers keep business logic out of handlers
    public bool IsLocked(DateTimeOffset now) =>
        LockedUntil.HasValue && LockedUntil.Value > now;

    public void RecordFailedAttempt(DateTimeOffset now, int maxAttempts, TimeSpan lockoutDuration)
    {
        FailedLoginAttempts++;
        if (FailedLoginAttempts >= maxAttempts)
            LockedUntil = now.Add(lockoutDuration);
    }

    public void ResetLockout()
    {
        FailedLoginAttempts = 0;
        LockedUntil = null;
    }
}
