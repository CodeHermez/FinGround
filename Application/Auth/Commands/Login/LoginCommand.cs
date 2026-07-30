using FinGround.Application.Auth.Common;
using MediatR;

namespace FinGround.Application.Auth.Commands.Login;

public record LoginCommand(string Email, string Password) : IRequest<AuthResultDto>;
