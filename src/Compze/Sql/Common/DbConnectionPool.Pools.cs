using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using Compze.Utilities.SystemCE.TransactionsCE;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Sql.Common;

abstract partial class DbConnectionPool<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   class DefaultDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : DbConnectionPool<TConnection, TCommand>, IDbConnectionPool<TConnection, TCommand>
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

   class TransactionAffinityDbConnectionPool(string connectionString, Func<string, TConnection> createConnection) : DefaultDbConnectionPool(connectionString, createConnection)
   {
      readonly IThreadShared<Dictionary<string, Task<TConnection>>> _transactionConnections =
         IThreadShared.WithDefaultTimeouts<Dictionary<string, Task<TConnection>>>();

      public override async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
      {
         var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

         if(transactionLocalIdentifier == null)
         {
            return await base.UseConnectionAsync(func).caf();
         } else
         {
            //TConnection requires that the same connection is used throughout a transaction
            var getConnectionTask = _transactionConnections.Update(
               transactionConnections => transactionConnections.GetOrAdd(
                  transactionLocalIdentifier,
                  constructor: async () =>
                  {
                     var connection = await OpenConnectionAsync().caf();
                     Transaction.Current!.OnCompleted(action: () => _transactionConnections.Update(transactionConnectionsAfterTransaction =>
                     {
                        transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
                        connection.Dispose();
                     }));
                     return connection;
                  }));

            var connection = await getConnectionTask.caf();
            return await func(connection).caf();
         }
      }

      public override TResult UseConnection<TResult>(Func<TConnection, TResult> func)
      {
         var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

         if(transactionLocalIdentifier == null)
         {
            return base.UseConnection(func);
         } else
         {
            //TConnection requires that the same connection is used throughout a transaction
            var getConnectionTask = _transactionConnections.Update(
               transactionConnections => transactionConnections.GetOrAdd(
                  transactionLocalIdentifier,
                  constructor: () =>
                  {
                     var createConnectionTask = Task.FromResult(OpenConnection());
                     Transaction.Current!.OnCompleted(action: () => _transactionConnections.Update(transactionConnectionsAfterTransaction =>
                     {
                        transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
                        createConnectionTask.Result.Dispose();
                     }));
                     return createConnectionTask;
                  }));

            return func(getConnectionTask.Result);
         }
      }
   }
}
