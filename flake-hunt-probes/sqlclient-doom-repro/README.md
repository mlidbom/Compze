# SqlClient dooms a connection that another transaction has taken from the pool

A repro for reporting to dotnet/SqlClient, from the 2026-11-26 hunt of CI run 45886748771. Compze's fix is e36a59f5d on
main: its pool subscribes the disposal of a transaction's connection only once the connection has opened.

- **What happens:** a `TransactionCompleted` handler subscribed before the connection opens runs before SqlClient's own
  handler. It closes the connection, and another transaction takes the same physical connection from the pool. SqlClient's
  handler for the first transaction then runs: `SqlDelegatedTransaction.TransactionEnded` calls `DoomThisConnection()`
  whichever transaction the connection is in by then. The second transaction's commit fails.
- **Where:** `TransactionEnded` dooms unconditionally in 6.1.3, 6.1.8, 7.1.2 and main.
- **The control,** `control.cs`, differs only in subscribing its handler after the open, so that SqlClient's handler runs
  first. Its second transaction commits.

## Running them

They are .NET file-based apps. Run them from a copy outside any repository, since `dotnet run` takes in a
`Directory.Build.props` above the file, with `COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING` set to a SQL
Server connection string that ends in `;`:

```
dotnet run repro.cs
dotnet run control.cs
```

`repro.cs`, with Microsoft.Data.SqlClient 6.1.3 on Windows, 2026-11-26:

```
second transaction's command: 1
second transaction failed: TransactionAbortedException: The transaction has aborted. / InvalidOperationException: The requested operation cannot be completed because the connection has been broken.
same physical connection: True
```

`control.cs`:

```
second transaction's command: 1
second transaction committed
same physical connection: True
```
