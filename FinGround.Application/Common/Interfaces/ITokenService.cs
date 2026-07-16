using FinGround.Domain.Entities;

namespace FinGround.Application.Common.Interfaces;

public interface ITokenService
{
    string GenerateToken(User user);
    DateTime ExpiresAt();
}
