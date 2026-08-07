namespace FinGround.McpServer.Api;

// Every list/query response DTO comes from the Application layer (see GlobalUsings.cs) so the
// contract is defined exactly once. The three records below are the exception: they are shapes
// declared in the API's controllers rather than in Application, and referencing the API project
// from here would be backwards.

/// <summary>The anonymous body returned by GET /api/health.</summary>
public sealed record HealthDto(string Status, DateTime Timestamp);

/// <summary>Mirrors BalanceResponse in API/Controllers/AccountsController.cs.</summary>
public sealed record BalanceResponse(Guid AccountId, decimal NewBalance);

/// <summary>Mirrors TransferResponse in API/Controllers/TransactionsController.cs.</summary>
public sealed record TransferResponse(Guid TransactionId);
