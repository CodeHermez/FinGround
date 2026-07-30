using FinGround.Application.Auth.Common;
using MediatR;

namespace FinGround.Application.Auth.Commands.Register;

public record RegisterCommand(
    string Email,
    string Password,
    string FullName
) : IRequest<AuthResultDto>;
