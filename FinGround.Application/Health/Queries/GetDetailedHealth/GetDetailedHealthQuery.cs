using MediatR;

namespace FinGround.Application.Health.Queries.GetDetailedHealth;

public record GetDetailedHealthQuery : IRequest<DetailedHealthDto>;
