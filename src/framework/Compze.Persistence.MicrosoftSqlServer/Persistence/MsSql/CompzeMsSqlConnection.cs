using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Persistence.MsSql;

interface ICompzeMsSqlConnection : IPoolableConnection, ICompzeDbConnection<SqlCommand>
{
   internal static ICompzeMsSqlConnection Create(string connString) => new CompzeMsSqlConnection(connString);

   sealed class CompzeMsSqlConnection : ICompzeMsSqlConnection
   {
      SqlConnection Connection { get; }

      internal CompzeMsSqlConnection(string connectionString) => Connection = new SqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();
      public SqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}