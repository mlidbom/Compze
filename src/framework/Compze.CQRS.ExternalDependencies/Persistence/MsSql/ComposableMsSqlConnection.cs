using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Persistence.MsSql;

interface IComposableMsSqlConnection : IPoolableConnection, IComposableDbConnection<SqlCommand>
{
   internal static IComposableMsSqlConnection Create(string connString) => new ComposableMsSqlConnection(connString);

   sealed class ComposableMsSqlConnection : IComposableMsSqlConnection
   {
      SqlConnection Connection { get; }

      internal ComposableMsSqlConnection(string connectionString) => Connection = new SqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand IComposableDbConnection.CreateCommand() => CreateCommand();
      public SqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}