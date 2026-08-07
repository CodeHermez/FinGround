using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FinGround.McpServer.Api;
using FinGround.McpServer.Tools;

namespace FinGround.McpServer;

public static class FinGroundMcpServerBuilderExtensions
{
    /// <summary>
    /// Omitting nulls keeps unset audit fields (notes, initiatedBy) and empty reconciliation
    /// detail out of every tool response, which adds up fast across paged results.
    /// </summary>
    private static readonly JsonSerializerOptions ToolJsonOptions = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,

        // The SDK calls MakeReadOnly() on these while building each tool's schema, which throws
        // unless a resolver is set explicitly. Inheriting the default one is not enough.
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static IMcpServerBuilder WithFinGroundTools(
        this IMcpServerBuilder builder, IConfiguration configuration, McpTransport transport)
    {
        var options = configuration.GetSection(McpServerSettings.SectionName).Get<McpServerSettings>()
                      ?? new McpServerSettings();

        builder.WithTools<AccountTools>(ToolJsonOptions)
               .WithTools<TransactionTools>(ToolJsonOptions)
               .WithTools<AuditTools>(ToolJsonOptions);

        // Absent from tools/list entirely when disabled, rather than failing at call time.
        if (options.EnableMoneyMovement)
            builder.WithTools<MoneyMovementTools>(ToolJsonOptions);

        // Over HTTP the caller supplies their own bearer token, so there is nothing to log in to.
        if (transport == McpTransport.Stdio && options.EnableLoginTool)
            builder.WithTools<AuthTools>(ToolJsonOptions);

        return builder;
    }
}
