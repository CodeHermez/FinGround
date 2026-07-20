namespace FinGround.Application.Auth.Common;

public record AuthResultDto(
    Guid UserId,
    string Email,
    string FullName,
    string Token,
    DateTime ExpiresAt
);
