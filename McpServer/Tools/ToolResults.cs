using FinGround.McpServer.Api;
using ModelContextProtocol;

namespace FinGround.McpServer.Tools;

/// <summary>
/// Envelope returned by the paged tools. Carries just enough paging metadata for a model to
/// decide whether to ask for more, plus an explicit hint so it does not have to infer it.
/// </summary>
public sealed record PagedToolResult<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    string? Hint);

internal static class ToolHelpers
{
    private const int MaxPageSize = 100;

    /// <summary>
    /// The API silently clamps oversized pages; clamping here too lets the tool report what it
    /// actually did instead of the model wondering why it got fewer rows than it asked for.
    /// </summary>
    public static (int Page, int PageSize) NormalisePaging(int page, int pageSize) =>
        (Math.Max(page, 1), Math.Clamp(pageSize, 1, MaxPageSize));

    public static PagedToolResult<T> ToToolResult<T>(this PagedResult<T> result) =>
        new(result.Items,
            result.TotalCount,
            result.Page,
            result.PageSize,
            result.TotalPages,
            result.HasNextPage
                ? $"Showing page {result.Page} of {result.TotalPages}. "
                  + $"Call again with page={result.Page + 1} for more."
                : null);

    /// <summary>Rejects a bad amount before it costs an API round trip.</summary>
    public static void ValidateAmount(decimal amount, decimal maxAmount, string operation)
    {
        if (amount <= 0)
            throw new McpException($"The {operation} amount must be greater than zero (got {amount}).");

        if (amount > maxAmount)
            throw new McpException(
                $"The {operation} amount {amount} exceeds this MCP server's configured limit of "
                + $"{maxAmount}. Raise McpServer:MaxTransactionAmount to allow larger amounts.");
    }
}
