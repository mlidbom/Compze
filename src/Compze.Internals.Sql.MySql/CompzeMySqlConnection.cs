using System.Data.Common;
using Compze.Internals.Sql.Common.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using MySqlConnector;

namespace Compze.Internals.Sql.MySql;

public interface ICompzeMySqlConnection : IPoolableConnection, ICompzeDbConnection<MySqlCommand>
{
   public static ICompzeMySqlConnection Create(string connString) => new CompzeMySqlConnection(connString);

   public sealed class CompzeMySqlConnection : ICompzeMySqlConnection
   {
      MySqlConnection Connection { get; }

      internal CompzeMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().caf();

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();
      public MySqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}
