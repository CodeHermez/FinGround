using System.Text;
using System.Threading.RateLimiting;
using FinGround.API.Middleware;
using FinGround.Application;
using FinGround.Infrastructure;
using FinGround.Infrastructure.Persistence;
using FinGround.Infrastructure.Persistence.Seeders;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddApplication();
// ── handle different infrastructure services such as db
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddScoped<DatabaseSeeder>();

// JWT 
var jwtSection = builder.Configuration.GetSection("Jwt");
var secretKey = jwtSection["SecretKey"]
    ?? throw new InvalidOperationException("JWT SecretKey is not configured.");

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
                                       Encoding.UTF8.GetBytes(secretKey)),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// rate limit partitioned per client IP checks x-forwarded-For first proxy-aware
// it will then falls back to the direct connection address.
static string GetClientIp(HttpContext ctx)
{
    var forwarded = ctx.Request.Headers["X-Forwarded-For"].FirstOrDefault();
    if (!string.IsNullOrWhiteSpace(forwarded))
        return forwarded.Split(',')[0].Trim();

    return ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}

builder.Services.AddRateLimiter(options =>
{
    // Shared rejection handler: 429 + Retry-After header + JSON body
    options.OnRejected = async (ctx, token) =>
    {
        ctx.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        ctx.HttpContext.Response.ContentType = "application/json";

        // Read the lease metadata exactly once — some limiters consume it on first access.
        int? retryAfterSeconds = ctx.Lease.TryGetMetadata(MetadataName.RetryAfter, out var ra)
            ? (int)ra.TotalSeconds
            : null;

        if (retryAfterSeconds.HasValue)
            ctx.HttpContext.Response.Headers.RetryAfter =
                retryAfterSeconds.Value.ToString();

        await ctx.HttpContext.Response.WriteAsJsonAsync(new
        {
            type = "https://httpstatuses.io/429",
            title = "Too Many Requests",
            status = 429,
            detail = "Rate limit exceeded. Please wait before retrying.",
            retryAfterSeconds = retryAfterSeconds
        }, token);
    };

    //login: token bucket — 5-token burst, +1 token per 12 s per IP
    options.AddPolicy("auth-login", ctx =>
        RateLimitPartition.GetTokenBucketLimiter(
            partitionKey: GetClientIp(ctx),
            factory: _ => new TokenBucketRateLimiterOptions
            {
                TokenLimit = 5,
                ReplenishmentPeriod = TimeSpan.FromSeconds(12),  // 1 token / 12 s = 5/min
                TokensPerPeriod = 1,
                AutoReplenishment = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    //register: 3 sign-ups per 10 minutes per IP
    options.AddPolicy("auth-register", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetClientIp(ctx),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 3,
                Window = TimeSpan.FromMinutes(10),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));
});

//Swagger & OpenAPI

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Banking API Sandbox",
        Version = "v1",
        Description = """
            A portfolio-grade REST API built with **ASP.NET Core 8** and strict **Clean Architecture**
            (Domain / Application / Infrastructure / API layers).

            ## Features

            | Area | Detail |
            |---|---|
            | **Auth** | JWT Bearer — register → receive token → pass as `Authorization: Bearer <token>` |
            | **Rate limiting** | Login: 5 req/min token-bucket · Register: 3 req/10 min fixed-window |
            | **Account lockout** | 5 failed logins → 15-minute lock · Admin can unlock manually |
            | **CQRS** | Every use-case is a MediatR `IRequest` — zero business logic in controllers |
            | **Audit logging** | Every deposit, withdrawal, and transfer leg writes a before/after balance snapshot |
            | **Reconciliation** | Replay an account's audit trail and verify the stored balance matches |
            | **Pagination** | All list endpoints return a `PagedResult<T>` envelope with `totalCount`, `totalPages`, `hasNextPage` |
            | **Filtering** | Accounts: accountNumber, balance range · Transactions: amount range, date range · Audit logs: accountId, command, initiatedBy, date range |

            ## Quick start

            1. **POST /api/auth/login** with `{"email":"demo@banking-sandbox.dev","password":"Demo1234!"}` — copy the `token` value.
            2. Click **Authorize** above and enter `Bearer <token>`.
            3. Explore any endpoint — all require authentication except `/api/health*`.

            ## Demo seed data

            | Credential | Role | Notes |
            |---|---|---|
            | `demo@banking-sandbox.dev` / `Demo1234!` | **Admin** | Can call `/api/admin/*` |
            | Self-registered accounts | User | Cannot call admin endpoints (403) |

            Two accounts are pre-seeded: `CHK-0001` (checking) and `SAV-0001` (savings),
            each with a completed deposit, withdrawal, and transfer so every audit endpoint
            returns meaningful data immediately.
            """
    });

    // Wire up the XML documentation file generated by the build
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath))
        c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);

    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Enter: Bearer {your JWT token}",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    };

    c.AddSecurityDefinition("Bearer", securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id   = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});




var app = builder.Build();

//auto-migrate and seed on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BankDbContext>();
    await db.Database.MigrateAsync();

    var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
    await seeder.SeedAsync();
}

// exception handling via middleware
app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
