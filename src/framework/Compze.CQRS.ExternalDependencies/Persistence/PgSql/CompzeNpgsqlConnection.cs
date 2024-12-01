using System.Data.Common;
using System.Threading.Tasks;
using Compze.Persistence.Common.AdoCE;
using Compze.SystemCE.ThreadingCE.TasksCE;
using Npgsql;

namespace Compze.Persistence.PgSql;

interface ICompzNpgsqlConnection : IPoolableConnection, ICompzDbConnection<NpgsqlCommand>
{
   //todo: Check if upgrade of Npgsql from 4.1.4 to 7.0.0 means that we should change something.
   //Npgsql 7.0 Release Notes | Npgsql Documentation https://www.npgsql.org/doc/release-notes/7.0.html
   //Verify Windows-only distributed transactions work on .NET 7.0 � Issue #4581 � npgsql/npgsql � GitHub https://github.com/npgsql/npgsql/issues/4581
   internal static ICompzNpgsqlConnection Create(string connString) => new CompzNpgsqlConnection(connString);

   sealed class CompzNpgsqlConnection(string connectionString) : ICompzNpgsqlConnection
   {
      NpgsqlConnection Connection { get; } = new(connectionString);

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().CaF();

      DbCommand ICompzDbConnection.CreateCommand() => CreateCommand();
      public NpgsqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}
