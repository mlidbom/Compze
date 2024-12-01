using System.Data.Common;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql;

interface IComposableMySqlConnection : IPoolableConnection, IComposableDbConnection<MySqlCommand>
{
   internal static IComposableMySqlConnection Create(string connString) => new ComposableMySqlConnection(connString);

   sealed class ComposableMySqlConnection : IComposableMySqlConnection
   {
      MySqlConnection Connection { get; }

      internal ComposableMySqlConnection(string connectionString) => Connection = new MySqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
      public MySqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}