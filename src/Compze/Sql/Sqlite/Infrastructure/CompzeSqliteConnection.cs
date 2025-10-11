using System.Data.Common;
using Compze.Sql.Common.Abstractions;
using Compze.Utilities.Threading.TasksCE;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Infrastructure;

internal interface ICompzeSqliteConnection : IPoolableConnection, ICompzeDbConnection<SqliteCommand>
{
   internal static ICompzeSqliteConnection Create(string connString) => new CompzeSqliteConnection(connString);

   sealed class CompzeSqliteConnection : ICompzeSqliteConnection
   {
      SqliteConnection Connection { get; }

      internal CompzeSqliteConnection(string connectionString) => Connection = new SqliteConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().caf();

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();
      public SqliteCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}
