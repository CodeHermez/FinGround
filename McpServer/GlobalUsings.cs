// The response contract is owned by the Application layer. Importing those namespaces globally
// keeps every tool and client method reading against the same DTOs the API itself returns.

global using FinGround.Application.Accounts.Queries.GetAccountById;
global using FinGround.Application.AuditLogs.Queries.GetAuditLogsByAccount;
global using FinGround.Application.AuditLogs.Queries.ReconcileAccount;
global using FinGround.Application.AuditLogs.Queries.ReconcileAllAccounts;
global using FinGround.Application.Auth.Common;
global using FinGround.Application.Common.Models;
global using FinGround.Application.Health.Queries.GetDetailedHealth;
global using FinGround.Application.Transactions.Queries.GetTransactionsByAccount;
