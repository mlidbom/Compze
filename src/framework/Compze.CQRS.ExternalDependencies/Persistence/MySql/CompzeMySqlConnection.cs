using System.Data.Common;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql;

interface ICompzMySqlConnection : IPoolableConnection, ICompzDbConnection<MySqlCommand>
{
   internal static ICompzMySqlConnection Create(string connString) => new CompzMySqlConnection(connString);

   sealed class CompzMySqlConnection : ICompzMySqlConnection
   {
      MySqlConnection Connection { get; }

      internal CompzMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand ICompzDbConnection.CreateCommand() => CreateCommand();
      public MySqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}