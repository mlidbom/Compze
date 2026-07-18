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

      internal CompzeMySqlConnection(string connectionString) => Connection = new MySqlConnection(WithoutXaTransactions(connectionString));

      ///<summary>Forces <see cref="MySqlConnectionStringBuilder.UseXaTransactions"/> off, whatever the supplied connection string says.</summary>
      ///<remarks>Every Compze unit of work is one connection enlisted in one ambient <c>System.Transactions</c> transaction and<br/>
      /// single-phase-committed - the <c>DbConnectionPool</c> holds exactly one connection per transaction, so a second resource,<br/>
      /// and thus a distributed (XA) transaction, is never needed. MySqlConnector defaults<br/>
      /// <see cref="MySqlConnectionStringBuilder.UseXaTransactions"/> to <c>true</c>; this keeps the plain <c>START TRANSACTION</c>/<c>COMMIT</c><br/>
      /// model the pool is built on and avoids XA's crash-recovery obligations (<c>XA RECOVER</c>, prepared transactions surviving a<br/>
      /// disconnect). Enforced here rather than in each connection string, so no configured string can opt back into XA.</remarks>
      static string WithoutXaTransactions(string connectionString) =>
         new MySqlConnectionStringBuilder(connectionString) {UseXaTransactions = false}.ConnectionString;

      public void Open() => Connection.Open();
      public async Task OpenAsync() => await Connection.OpenAsync().caf();

      DbCommand ICompzeDbConnection.CreateCommand() => CreateCommand();
      public MySqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}
