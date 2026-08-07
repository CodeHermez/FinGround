using System.ComponentModel;
using FinGround.McpServer.Api;
using ModelContextProtocol.Server;

namespace FinGround.McpServer.Tools;

/// <summary>
/// Only registered on the stdio transport. Over HTTP the caller's own bearer token is forwarded,
/// so there is nothing for this tool to do.
/// </summary>
[McpServerToolType]
public sealed class AuthTools(StdioTokenProvider tokens)
{
    [McpServerTool(
        Name = "login",
        Title = "Log in to FinGround",
        ReadOnly = false,
        Destructive = false,
        Idempotent = true,
        OpenWorld = false)]
    [Description("Authenticate against the FinGround API and cache the session for the rest of this "
        + "MCP session. Only needed if the server was started without credentials configured. "
        + "The sandbox demo login is demo@banking-sandbox.dev / Demo1234!. Note the API throttles "
        + "login attempts per IP, so avoid retrying in a loop.")]
    public async Task<LoginResult> LoginAsync(
        [Description("The user's email address.")] string email,
        [Description("The user's password.")] string password,
        CancellationToken ct = default)
    {
        var result = await tokens.LoginAsync(email, password, ct);

        // Deliberately no token in the response — it would land in the model's context and in
        // any saved transcript. It is held in memory and attached to outbound calls instead.
        return new LoginResult(result.UserId, result.Email, result.FullName, result.ExpiresAt);
    }
}

public sealed record LoginResult(Guid UserId, string Email, string FullName, DateTime ExpiresAt);
