using MediatR;

namespace FinGround.Application.Admin.Queries.GetUserDetail;

/// <summary>
/// Returns full detail for a single user: profile fields, lockout state,
/// and every audit log entry attributed to them across all accounts.
/// Intended for admin review before deciding whether to unlock an account.
/// </summary>
public record GetUserDetailQuery(Guid UserId) : IRequest<UserDetailDto>;
