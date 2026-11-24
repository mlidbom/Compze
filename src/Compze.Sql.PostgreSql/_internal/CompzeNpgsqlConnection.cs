using System.Transactions;
using Compze.Contracts;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Sql.PostgreSql._private;
using Npgsql;

namespace Compze.Sql.PostgreSql._internal;

interface ICompzeNpgsqlConnection : IPoolableConnection, ICompzeDbConnection<NpgsqlCommand>
{
   //todo:review: Check if upgrade of Npgsql from 4.1.4 to 7.0.0 means that we should change something.
   //Npgsql 7.0 Release Notes | Npgsql Documentation https://www.npgsql.org/doc/release-notes/7.0.html
   //Verify Windows-only distributed transactions work on .NET 7.0 � Issue #4581 � npgsql/npgsql � GitHub https://github.com/npgsql/npgsql/issues/4581
   public static ICompzeNpgsqlConnection Create(string connString) => new CompzeNpgsqlConnection(connString);

   ///<summary>Compze's connection to a PostgreSQL database: an <see cref="NpgsqlConnection"/> that passes the <see cref="NpgsqlPoolGate"/> of<br/>
   /// its pool as it opens, and gives its place back as it is disposed. Opened in an ambient transaction, it begins a transaction of its<br/>
   /// own, which takes part in the ambient one as its decision (<see cref="PgSqlTransactionAsTheDecision"/>), and every command it<br/>
   /// creates runs in it.</summary>
   public sealed class CompzeNpgsqlConnection : ICompzeNpgsqlConnection
   {
      readonly NpgsqlConnection _connection;
      readonly NpgsqlPoolGate _poolGate;
      bool _holdsAPlaceInThePool;
      PgSqlTransactionAsTheDecision? _transaction;

      internal CompzeNpgsqlConnection(string connectionString)
      {
         _connection = new NpgsqlConnection(connectionString);
         _poolGate = NpgsqlPoolGate.OfThePoolFor(connectionString);
      }

      public void Open()
      {
         var ambientTransaction = Transaction.Current;
         _poolGate.Take();
         _holdsAPlaceInThePool = true;
         _connection.Open();
         if(ambientTransaction != null) _transaction = new PgSqlTransactionAsTheDecision(PgSqlTransactionAsTheDecision.BeginDatabaseTransaction(_connection, ambientTransaction), ambientTransaction);
      }

      public async Task OpenAsync()
      {
         var ambientTransaction = Transaction.Current;
         await _poolGate.TakeAsync().caf();
         _holdsAPlaceInThePool = true;
         await _connection.OpenAsync().caf();
         if(ambientTransaction != null) _transaction = new PgSqlTransactionAsTheDecision(await PgSqlTransactionAsTheDecision.BeginDatabaseTransactionAsync(_connection, ambientTransaction).caf(), ambientTransaction);
      }

      public NpgsqlCommand CreateCommand()
      {
         var command = _connection.CreateCommand();
         _transaction?.Includes(command);
         return command;
      }

      public void Dispose()
      {
         AssertTheTransactionHasEnded();
         _transaction?.Dispose();
         _connection.Dispose();
         GiveBackItsPlaceInThePool();
      }

      public async ValueTask DisposeAsync()
      {
         AssertTheTransactionHasEnded();
         _transaction?.Dispose();
         await _connection.DisposeAsync().caf();
         GiveBackItsPlaceInThePool();
      }

      void AssertTheTransactionHasEnded() =>
         Contract.State.Assert(_transaction is not { HasEnded: false }, () => "A PostgreSQL connection was disposed while its transaction hadn't committed or rolled back.");

      ///<summary>Gives back its place in the pool, if it holds one: once its connection, disposed, is back in Npgsql's pool.</summary>
      void GiveBackItsPlaceInThePool()
      {
         if(!_holdsAPlaceInThePool) return;
         _holdsAPlaceInThePool = false;
         _poolGate.Release();
      }
   }
}
