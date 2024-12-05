using System.Data.Common;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql;

interface ICompzeMySqlConnection : IPoolableConnection, ICompzeDbConnection<MySqlCommand>
{
   internal static ICompzeMySqlConnection Create(string connString) => new CompzeMySqlConnection(connString);

   sealed class CompzeMySqlConnection : ICompzeMySqlConnection
   {
      MySqlConnection Connection { get; }

      internal CompzeMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();
      public MySqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}