using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using System.Transactions;
using Composable.SystemCE;
using Composable.SystemCE.CollectionsCE.GenericCE;
using Composable.SystemCE.DiagnosticsCE;
using Composable.SystemCE.ThreadingCE;
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

      static readonly TimingsStatisticsCollector SubsequentOpenings = StopwatchCE.CreateCollector("Non-First connection openings",
                                                                                                  1.Milliseconds(),
                                                                                                  2.Milliseconds(),
                                                                                                  3.Milliseconds(),
                                                                                                  4.Milliseconds(),
                                                                                                  5.Milliseconds(),
                                                                                                  10.Milliseconds(),
                                                                                                  20.Milliseconds()
      );

      static int _openings = 0;
      protected async Task<TConnection> OpenConnectionAsyncFlex(SyncOrAsync syncOrAsync)
      {
         using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
         var connection = _createConnection(_connectionString);

         //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
         //first opening of a connection that takes very long and thus trying our own pooling will not help.
         if(Interlocked.Increment(ref _openings) == 1)
         {
            await connection.OpenAsyncFlex(syncOrAsync).CaF();//Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
         } else
         {
            //Currently about 900 passing tests. Total of 13 seconds. Average per test of 12ms.
            //Our own pooling will NOT beat 12ms for all the connection usage in a db based test.
            //Remember, this includes cleaning databases, creating tables, inserting data, reading the data etc.
            //This is in other words probably less than 1ms per connection usage that will make at least one
            //roundtrip to the db server in any case, likely eclipsing 1ms.
            await SubsequentOpenings.TimeAsyncFlex(syncOrAsync, connection.OpenAsyncFlex).CaF();
         }
         return connection;
      }


      public virtual async Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<TConnection, Task<TResult>> func)
      {
         var connection = await OpenConnectionAsyncFlex(syncOrAsync).CaF();
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

      public override async Task<TResult> UseConnectionAsyncFlex<TResult>(SyncOrAsync syncOrAsync, Func<TConnection, Task<TResult>> func)
      {
         var transactionLocalIdentifier = Transaction.Current?.TransactionInformation.LocalIdentifier;

         if(transactionLocalIdentifier == null)
         {
            return await base.UseConnectionAsyncFlex(syncOrAsync, func).CaF();
         } else
         {
            //TConnection requires that the same connection is used throughout a transaction
            var getConnectionTask = _transactionConnections.Update(
               transactionConnections => transactionConnections.GetOrAdd(
                  transactionLocalIdentifier,
                  constructor: () =>
                  {
                     var createConnectionTask = OpenConnectionAsyncFlex(syncOrAsync);
                     Transaction.Current!.OnCompleted(action: () => _transactionConnections.Update(transactionConnectionsAfterTransaction =>
                     {
                        transactionConnectionsAfterTransaction.Remove(transactionLocalIdentifier);
                        createConnectionTask.Result.Dispose();
                     }));
                     return createConnectionTask;
                  }));

            var connection = await getConnectionTask.CaF();
            return await func(connection).CaF();
         }
      }
   }
}