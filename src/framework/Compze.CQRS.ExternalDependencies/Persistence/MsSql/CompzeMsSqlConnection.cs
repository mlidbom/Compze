using System.Data.Common;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Persistence.MsSql;

interface ICompzMsSqlConnection : IPoolableConnection, ICompzDbConnection<SqlCommand>
{
   internal static ICompzMsSqlConnection Create(string connString) => new CompzMsSqlConnection(connString);

   sealed class CompzMsSqlConnection : ICompzMsSqlConnection
   {
      SqlConnection Connection { get; }

      internal CompzMsSqlConnection(string connectionString) => Connection = new SqlConnection(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand ICompzDbConnection.CreateCommand() => CreateCommand();
      public SqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}