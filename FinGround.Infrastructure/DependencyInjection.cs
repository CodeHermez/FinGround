using FinGround.Application.Common.Interfaces;
using FinGround.Application.Interfaces;
using FinGround.Infrastructure.Persistence;
using FinGround.Infrastructure.Services;
using FinGround.Infrastructure.Persistence;
using FinGround.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FinGround.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<BankDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions => npgsqlOptions.MigrationsAssembly(
                    typeof(BankDbContext).Assembly.GetName().Name)));

        services.AddScoped<IBankDbContext>(provider =>
            provider.GetRequiredService<BankDbContext>());

        services.AddScoped<IDatabaseHealthChecker, DatabaseHealthChecker>();

        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();

        return services;
    }
}
