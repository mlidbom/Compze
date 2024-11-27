using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ThreadingCE.ResourceAccess;
using Composable.SystemCE.ThreadingCE.TasksCE;
using Composable.SystemCE.TransactionsCE;

// ReSharper disable StaticMemberInGenericType

namespace Composable.Persistence.Common.AdoCE;

abstract partial class DbConnectionManager<TConnection, TCommand>
   where TConnection : IPoolableConnection, IComposableDbConnection<TCommand>
   where TCommand : DbCommand
{
   class DefaultDbConnectionManager : DbConnectionManager<TConnection, TCommand>, IDbConnectionPool<TConnection, TCommand>
   {
      readonly string _connectionString;
      readonly Func<string, TConnection> _createConnection;

      public DefaultDbConnectionManager(string connectionString, Func<string, TConnection> createConnection)
      {
         _connectionString = connectionString;
         _createConnection = createConnection;
      }

      static int _openings = 0;

      protected async Task<TConnection> OpenConnectionAsync()
      {
         using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
         var connection = _createConnection(_connectionString);

         //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
         //first opening of a connection that takes very long and thus trying our own pooling will not help.
         if(Interlocked.Increment(ref _openings) == 1)
         {
            await connection.OpenAsync().CaF(); //Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
         } else
         {
            await connection.OpenAsync().CaF();
         }

         return connection;
      }

      protected TConnection OpenConnection()
      {
         using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
         var connection = _createConnection(_connectionString);

         //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
         //first opening of a connection that takes very long and thus trying our own pooling will not help.
         if(Interlocked.Increment(ref _openings) == 1)
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
         var connection = await OpenConnectionAsync().CaF();
         await using var connection1 = connection.CaF();
         return await func(connection).CaF();
      }

      public override string ToString() => _connectionString;
   }

   class TransactionAffinityDbConnectionManager : DefaultDbConnectionManager
   {
      readonly IThreadShared<Dictionary<string, Task<TConnection>>> _transactionConnections =
         ThreadShared.WithDefaultTimeout<Dictionary<string, Task<TConnection>>>();

      public TransactionAffinityDbConnectionManager(string connectionString, Func<string, TConnection> createConnection) : base(connectionString, createConnection) {}

      public override async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
      {
         var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

         if(transactionLocalIdentifier == null)
         {
            return await base.UseConnectionAsync(func).CaF();
         } else
         {
            //TConnection requires that the same connection is used throughout a transaction
            var getConnectionTask = _transactionConnections.Update(
               transactionConnections => transactionConnections.GetOrAdd(
                  transactionLocalIdentifier,
                  constructor: async () =>
                  {
                     var connection = await OpenConnectionAsync().CaF();
                     Transaction.Current!.OnCompleted(action: () => _transactionConnections.Update(transactionConnectionsAfterTransaction =>
                     {
                        transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
                        connection.Dispose();
                     }));
                     return connection;
                  }));

            var connection = await getConnectionTask.CaF();
            return await func(connection).CaF();
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
