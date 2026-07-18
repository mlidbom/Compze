using System.Transactions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ThreadingCE.Async;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Internals.SystemCE.TransactionsCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Internals.Sql.Common;

public abstract partial class DbConnectionPool<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   public class DefaultDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : DbConnectionPool<TConnection, TCommand>, IDbConnectionPool<TConnection, TCommand>
   {
      readonly string _connectionString = connectionString;
      readonly Func<string, TConnection> _createConnection = createConnection;

      static readonly RunOnce RunOnce = new();

      protected async Task<TConnection> OpenConnectionAsync()
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

      protected TConnection OpenConnection()
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

      public virtual TResult UseConnection<TResult>(Func<TConnection, TResult> func)
      {
         using var connection = OpenConnection();
         return func(connection);
      }

      public virtual async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
      {
         var connection = await OpenConnectionAsync().caf();
         //makes sure DisposeAsync is called without capturing sync context. We can't do it inline because then connection would be ConfiguredAsyncDisposable, not TConnection
         await using var _ = connection.caf();
         return await func(connection).caf();
      }

      public override string ToString() => _connectionString;
   }

   ///<summary>A pool whose every operation under one transaction runs on ONE shared connection, and a connection does exactly one<br/>
   /// thing at a time. Each transaction's connection is held as an <see cref="IAsyncShared{TConnection}"/>, so concurrent operations<br/>
   /// under one transaction — several async calls awaited together, say — serialize onto the connection instead of colliding on it: a<br/>
   /// collision no provider actually supports and that MySQL rejects outright ("already an open DataReader"). The serialization is<br/>
   /// re-entrant per async flow, so a nested <see cref="UseConnection{TResult}"/> on the same flow does not deadlock while independent<br/>
   /// flows serialize.</summary>
   public class TransactionAffinityDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : DefaultDbConnectionPool(connectionString, createConnection)
   {
      readonly IThreadShared<Dictionary<string, IAsyncShared<TConnection>>> _transactionConnections =
         IThreadShared.New(new Dictionary<string, IAsyncShared<TConnection>>());

      public override async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
      {
         var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

         if(transactionLocalIdentifier == null)
         {
            return await base.UseConnectionAsync(func).caf();
         }

         return await GetOrOpenSharedConnection(transactionLocalIdentifier, () => OpenConnectionAsync()).LockedAsync(func).caf();
      }

      public override TResult UseConnection<TResult>(Func<TConnection, TResult> func)
      {
         var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

         if(transactionLocalIdentifier == null)
         {
            return base.UseConnection(func);
         }

         return GetOrOpenSharedConnection(transactionLocalIdentifier, () => Task.FromResult(OpenConnection())).Locked(func);
      }

      IAsyncShared<TConnection> GetOrOpenSharedConnection(string transactionLocalIdentifier, Func<Task<TConnection>> openConnection) =>
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
   }
}
