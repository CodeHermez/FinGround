using System.Text;
using FinGround.McpServer;
using FinGround.McpServer.Api;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

// Two transports, one project: stdio for local MCP clients (Claude Code, Claude Desktop) and
// streamable HTTP for remote agents. The tool set and API client are identical; only the host
// type and the source of the bearer token differ.

const string StdioFlag = "--stdio";

var useStdio =
    args.Contains(StdioFlag, StringComparer.OrdinalIgnoreCase) ||
    string.Equals(Environment.GetEnvironmentVariable("FINGROUND_MCP_TRANSPORT"), "stdio",
                  StringComparison.OrdinalIgnoreCase);

// The command-line configuration provider throws FormatException on a valueless switch,
// so the flag has to be stripped before the host builder ever sees it.
var hostArgs = args.Where(a => !a.Equals(StdioFlag, StringComparison.OrdinalIgnoreCase)).ToArray();

if (useStdio)
{
    // An MCP client launches this process with its own working directory, so the content root has
    // to be pinned to the assembly location or appsettings.json is silently never found.
    var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
    {
        Args = hostArgs,
        ContentRootPath = AppContext.BaseDirectory
    });

    // stdout carries JSON-RPC frames. A single log line written there corrupts the stream and the
    // client drops the connection, so every provider is replaced with a stderr-only console.
    builder.Logging.ClearProviders();
    builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

    builder.Services.AddFinGroundApiClient(builder.Configuration, McpTransport.Stdio);

    builder.Services
        .AddMcpServer(o => o.ServerInfo = new() { Name = "finground", Version = "1.0.0" })
        .WithStdioServerTransport()
        .WithFinGroundTools(builder.Configuration, McpTransport.Stdio);

    await builder.Build().RunAsync();
}
else
{
    // Same reason as the stdio branch: without pinning the content root, running the DLL from the
    // repo root silently skips appsettings.json and Kestrel falls back to the default port 5000 —
    // which is the API's port.
    var builder = WebApplication.CreateBuilder(new WebApplicationOptions
    {
        Args = hostArgs,
        ContentRootPath = AppContext.BaseDirectory
    });

    var settings = builder.Configuration.GetSection(McpServerSettings.SectionName)
                          .Get<McpServerSettings>() ?? new McpServerSettings();

    builder.Services.AddFinGroundApiClient(builder.Configuration, McpTransport.Http);

    builder.Services
        .AddMcpServer(o => o.ServerInfo = new() { Name = "finground", Version = "1.0.0" })
        .WithHttpTransport(o =>
        {
            // Already the default as of the 2026-07-28 protocol revision; set explicitly because
            // tool handlers must run on the HTTP request's ExecutionContext for
            // HttpContextTokenProvider to see the caller's Authorization header. The stateful
            // escape hatch (PerSessionExecutionContext) is obsolete and would break that.
            o.Stateless = true;
        })
        .WithFinGroundTools(builder.Configuration, McpTransport.Http)
        .AddAuthorizationFilters();

    // Optional defence in depth: reject invalid tokens here rather than forwarding them and
    // letting the API 401. The API remains the sole authority on what a token may actually do.
    if (settings.RequireAuth)
    {
        var jwt = builder.Configuration.GetSection("Jwt");
        var secretKey = jwt["SecretKey"]
            ?? throw new InvalidOperationException(
                "McpServer:RequireAuth is true but Jwt:SecretKey is not configured. "
                + "Set the Jwt__SecretKey environment variable to the same key the API uses, "
                + "or set McpServer:RequireAuth to false to pass tokens straight through.");

        builder.Services.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwt["Issuer"],
                ValidAudience = jwt["Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        builder.Services.AddAuthorization();
    }

    var app = builder.Build();

    if (settings.RequireAuth)
    {
        app.UseAuthentication();
        app.UseAuthorization();
    }

    // Always pass the route pattern explicitly.
    var mcp = app.MapMcp("/mcp");

    if (settings.RequireAuth)
        mcp.RequireAuthorization();

    await app.RunAsync();
}
