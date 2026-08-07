namespace FinGround.McpServer.Api;

/// <summary>
/// Binds the "FinGroundApi" configuration section.
/// Credentials belong in environment variables (FinGroundApi__Email, FinGroundApi__Password,
/// FinGroundApi__BearerToken), not in appsettings.json.
/// </summary>
public sealed class FinGroundApiOptions
{
    public const string SectionName = "FinGroundApi";

    public string BaseUrl { get; set; } = "http://localhost:5000";

    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>Pre-issued JWT. Used by the stdio transport when no login has happened yet.</summary>
    public string? BearerToken { get; set; }

    /// <summary>Optional credentials the stdio transport uses to log in on demand.</summary>
    public string? Email { get; set; }

    public string? Password { get; set; }
}

/// <summary>
/// Binds the "McpServer" configuration section. Named ...Settings rather than ...Options to avoid
/// colliding with ModelContextProtocol.Server.McpServerOptions.
/// </summary>
public sealed class McpServerSettings
{
    public const string SectionName = "McpServer";

    /// <summary>Validate the caller's JWT locally before the request reaches a tool (HTTP transport only).</summary>
    public bool RequireAuth { get; set; } = true;

    /// <summary>When false, the deposit/withdraw/transfer/create_account tools are not registered at all.</summary>
    public bool EnableMoneyMovement { get; set; } = true;

    /// <summary>When false, the login tool is not registered. Always off for the HTTP transport.</summary>
    public bool EnableLoginTool { get; set; } = true;

    /// <summary>Upper bound on any single deposit/withdrawal/transfer, rejected before the call leaves this process.</summary>
    public decimal MaxTransactionAmount { get; set; } = 10_000m;
}
