using System.Transactions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;
using Compze.Internals.SystemCE.TransactionsCE;
using Compze.Internals.SystemCE.TransactionsCE._internal;

// ReSharper disable StaticMemberInGenericType

namespace Compze.Sql.Common._internal;

///<summary>Every operation under one transaction runs on ONE shared connection, and a connection does exactly one thing at a<br/>
/// time. Outside a transaction each operation opens its own fresh connection; inside a transaction it runs on the transaction's<br/>
/// <see cref="TransactionSpecificConnection"/>, which serializes the transaction's operations on it.</summary>
///<remarks>One connection per transaction is the only correct model, so there is no per-backend variation — every backend uses<br/>
/// this pool. Two connections in one transaction is a distributed transaction — a separate database session each — which every<br/>
/// supported database refuses without a coordinator (each of Compze's connections is a transaction's durable participant, of which<br/>
/// .NET allows one unless implicit distributed transactions are enabled), and a coordinator (MSDTC) is Windows-only and defeats the<br/>
/// cross-platform goal.</remarks>
partial class DbConnectionPool<TConnection, TCommand> : IDbConnectionPool<TConnection, TCommand>
   where TConnection : IPoolableConnection, ICompzeDbConnection<TCommand>
   where TCommand : DbCommand
{
   readonly string _connectionString;
   readonly Func<string, TConnection> _createConnection;

   ///<summary>The connection of each transaction using the pool, by the transaction's local identifier. Its lock guards the dictionary<br/>
   /// only: no connection opens or is disposed under it (see <see cref="TransactionSpecificConnection"/>).</summary>
   readonly IThreadShared<Dictionary<string, TransactionSpecificConnection>> _transactionConnections =
      IThreadShared.New(new Dictionary<string, TransactionSpecificConnection>());

   static readonly RunOnce RunOnce = new();

   DbConnectionPool(string connectionString, Func<string, TConnection> createConnection)
   {
      _connectionString = connectionString;
      _createConnection = createConnection;
   }

   public TResult UseConnection<TResult>(Func<TConnection, TResult> func) =>
      Transaction.Current is {} transaction
         ? ConnectionOf(transaction).Locked(func)
         : UseOwnFreshConnection(func);

   public async Task<TResult> UseConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func) =>
      Transaction.Current is {} transaction
         ? await ConnectionOf(transaction).LockedAsync(func).caf()
         : await UseOwnFreshConnectionAsync(func).caf();

   ///<summary>PROBE: told when the operations a caller ran on a connection have all returned, before the connection is let go.</summary>
   internal static Action<object>? ProbeOperationsReturned;

   TResult UseOwnFreshConnection<TResult>(Func<TConnection, TResult> func)
   {
      using var connection = OpenConnection();
      try
      {
         return func(connection);
      }
      finally
      {
         ProbeOperationsReturned?.Invoke(connection);
      }
   }

   async Task<TResult> UseOwnFreshConnectionAsync<TResult>(Func<TConnection, Task<TResult>> func)
   {
      var connection = await OpenConnectionAsync().caf();
      //makes sure DisposeAsync is called without capturing sync context. We can't do it inline because then connection would be ConfiguredAsyncDisposable, not TConnection
      await using var _ = connection.caf();
      try
      {
         return await func(connection).caf();
      }
      finally
      {
         ProbeOperationsReturned?.Invoke(connection);
      }
   }

   TConnection OpenConnection()
   {
      using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
      var connection = _createConnection(_connectionString);
      try
      {
         //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
         //first opening of a connection that takes very long and thus trying our own pooling will not help.
         if(RunOnce.IsFirstCall())
         {
            connection.Open(); //Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
         } else
         {
            connection.Open();
         }
      }
      catch
      {
         //Opening can fail after the database connection opened, such as when the ambient transaction refuses the connection's transaction.
         connection.Dispose();
         throw;
      }

      return connection;
   }

   async Task<TConnection> OpenConnectionAsync()
   {
      using var escalationForbidden = TransactionCE.NoTransactionEscalationScope($"Opening {typeof(TConnection)}");
      var connection = _createConnection(_connectionString);
      try
      {
         //This is here so that we can reassure ourselves, via a profiler, time and time again that it is actually the
         //first opening of a connection that takes very long and thus trying our own pooling will not help.
         if(RunOnce.IsFirstCall())
         {
            await connection.OpenAsync().caf(); //Currently 120 passing tests total of 60 seconds runtime, average per test 500ms.
         } else
         {
            await connection.OpenAsync().caf();
         }
      }
      catch
      {
         //Opening can fail after the database connection opened, such as when the ambient transaction refuses the connection's transaction.
         await connection.DisposeAsync().caf();
         throw;
      }

      return connection;
   }

   ///<summary>The connection of <paramref name="transaction"/>, which the pool registers for it on its first use, and removes as it<br/>
   /// completes. The connection disposes itself as the transaction completes: see <see cref="TransactionSpecificConnection"/>.</summary>
   TransactionSpecificConnection ConnectionOf(Transaction transaction)
   {
      var transactionLocalIdentifier = transaction.TransactionInformation.LocalIdentifier;
      return _transactionConnections.Locked(transactionConnections => transactionConnections.GetOrAdd(
                                                transactionLocalIdentifier,
                                                constructor: () =>
                                                {
                                                   DurableResourcesOfTransactions.TakesPart(transaction, DurableResource.ThatCanShareItsTransaction($"a database connection, {typeof(TConnection).Name}"));
                                                   transaction.OnCompleted(action: () => ForgetTheConnectionOfTheCompletedTransaction(transactionLocalIdentifier));
                                                   return new TransactionSpecificConnection(this, transaction);
                                                }));
   }

   ///<summary>Removes the connection of the transaction that <paramref name="transactionLocalIdentifier"/> identifies, which has completed.</summary>
   void ForgetTheConnectionOfTheCompletedTransaction(string transactionLocalIdentifier) =>
      _transactionConnections.Locked(transactionConnections => transactionConnections.Remove(transactionLocalIdentifier));

   public override string ToString() => _connectionString;
}
