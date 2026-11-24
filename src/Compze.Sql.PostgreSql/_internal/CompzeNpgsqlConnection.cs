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
   /// its pool as it opens, and gives its place back as it is disposed.</summary>
   public sealed class CompzeNpgsqlConnection : ICompzeNpgsqlConnection
   {
      readonly NpgsqlConnection _connection;
      readonly NpgsqlPoolGate _poolGate;
      bool _holdsAPlaceInThePool;

      internal CompzeNpgsqlConnection(string connectionString)
      {
         _connection = new NpgsqlConnection(connectionString);
         _poolGate = NpgsqlPoolGate.OfThePoolFor(connectionString);
      }

      public void Open()
      {
         _poolGate.Take();
         _holdsAPlaceInThePool = true;
         _connection.Open();
      }

      public async Task OpenAsync()
      {
         await _poolGate.TakeAsync().caf();
         _holdsAPlaceInThePool = true;
         await _connection.OpenAsync().caf();
      }

      public NpgsqlCommand CreateCommand() => _connection.CreateCommand();

      public void Dispose()
      {
         _connection.Dispose();
         GiveBackItsPlaceInThePool();
      }

      public async ValueTask DisposeAsync()
      {
         await _connection.DisposeAsync().caf();
         GiveBackItsPlaceInThePool();
      }

      ///<summary>Gives back its place in the pool, if it holds one: once its connection, disposed, is back in Npgsql's pool.</summary>
      void GiveBackItsPlaceInThePool()
      {
         if(!_holdsAPlaceInThePool) return;
         _holdsAPlaceInThePool = false;
         _poolGate.Release();
      }
   }
}
