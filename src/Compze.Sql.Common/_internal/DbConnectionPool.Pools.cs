using System.Transactions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Internals.SystemCE.TransactionsCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Sql.Common._internal;

///<summary>Every operation under one transaction runs on ONE shared connection, and a connection does exactly one thing at a<br/>
/// time. Outside a transaction each operation opens its own fresh connection; inside a transaction the connection is held as an<br/>
/// <see cref="IAsyncShared{TConnection}"/>, so concurrent operations under one transaction — several async calls awaited together,<br/>
/// say — serialize onto the connection instead of colliding on it: a collision no provider actually supports and that MySQL<br/>
/// rejects outright ("already an open DataReader"). The serialization is re-entrant per async flow, so a nested<br/>
/// <see cref="UseConnection{TResult}"/> on the same flow does not deadlock while independent flows serialize.</summary>
///<remarks>One connection per transaction is the only correct model, so there is no per-backend variation — every backend uses<br/>
/// this pool. Two connections in one transaction is a distributed transaction — a separate database session each — which every<br/>
/// supported provider refuses without a coordinator (MySQL and Npgsql outright; SqlClient unless implicit distributed<br/>
/// transactions are enabled), and a coordinator (MSDTC) is Windows-only and defeats the cross-platform goal.</remarks>
partial class DbConnectionPool<TConnection, TCommand> : IDbConnectionPool<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   readonly string _connectionString;
   readonly Func<string, TConnection> _createConnection;

   readonly IThreadShared<Dictionary<string, IAsyncShared<TConnection>>> _transactionConnections =
      IThreadShared.New(new Dictionary<string, IAsyncShared<TConnection>>());

   static readonly RunOnce RunOnce = new();

   DbConnectionPool(string connectionString, Func<string, TConnection> createConnection)
   {
      _connectionString = connectionString;
      _createConnection = createConnection;
   }

   public TResult UseConnection<TResult>(Func<TConnection, TResult> func)
   {
      var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

      if(transactionLocalIdentifier == null)
      {
         return UseOwnFreshConnection(func);
      }

#pragma warning disable CA1849 // The synchronous UseConnection path must open synchronously; Task.FromResult bridges that sync open into the Task-backed IAsyncShared the async path also feeds.
      return GetOrOpenTransactionSpecificConnection(transactionLocalIdentifier, () => Task.FromResult(OpenConnection())).Locked(func);
#pragma warning restore CA1849
   }

   public async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
   {
      var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

      if(transactionLocalIdentifier == null)
      {
         return await UseOwnFreshConnectionAsync(func).caf();
      }

      return await GetOrOpenTransactionSpecificConnection(transactionLocalIdentifier, OpenConnectionAsync).LockedAsync(func).caf();
   }

   TResult UseOwnFreshConnection<TResult>(Func<TConnection, TResult> func)
   {
      using var connection = OpenConnection();
      return func(connection);
   }

   async Task<TResult> UseOwnFreshConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
   {
      var connection = await OpenConnectionAsync().caf();
      //makes sure DisposeAsync is called without capturing sync context. We can't do it inline because then connection would be ConfiguredAsyncDisposable, not TConnection
      await using var _ = connection.caf();
      return await func(connection).caf();
   }

   TConnection OpenConnection()
   {
      using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
      var connection = _createConnection(_connectionString);

      //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
      //first opening of a connection that takes very long and thus trying our own pooling will not help.
      if(RunOnce.IsFirstCall())
      {
         connection.Open(); //Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
      } else
      {
         connection.Open();
      }

      return connection;
   }

   async Task<TConnection> OpenConnectionAsync()
   {
      using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
      var connection = _createConnection(_connectionString);

      //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
      //first opening of a connection that takes very long and thus trying our own pooling will not help.
      if(RunOnce.IsFirstCall())
      {
         await connection.OpenAsync().caf(); //Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
      } else
      {
         await connection.OpenAsync().caf();
      }

      return connection;
   }

   IAsyncShared<TConnection> GetOrOpenTransactionSpecificConnection(string transactionLocalIdentifier, Func<Task<TConnection>> openConnection) =>
      _transactionConnections.Locked(
         transactionConnections => transactionConnections.GetOrAdd(
            transactionLocalIdentifier,
            constructor: () =>
            {
               var sharedConnection = IAsyncShared.New(openConnection());
               Transaction.Current!.OnCompleted(action: () => _transactionConnections.Locked(transactionConnectionsAfterTransaction =>
               {
                  if(transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier, out var completed))
                  {
                     completed.Locked(connection => connection.Dispose());
                     completed.Dispose();
                  }
               }));
               return sharedConnection;
            }));

   public override string ToString() => _connectionString;
}
