using Compze.Sql.Common._internal;
using Compze.Sql.Common._internal.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Microsoft.Data.SqlClient;

namespace Compze.Sql.MicrosoftSql._internal;

interface ICompzeMsSqlConnection : IPoolableConnection, ICompzeDbConnection<SqlCommand>
{
   public static ICompzeMsSqlConnection Create(string connString) => new CompzeMsSqlConnection(connString);

   ///<summary>A connection to SQL Server. Opened in an ambient transaction, it takes part in it as its one durable resource, through<br/>
   /// SqlClient's own enlistment, which commits as the transaction's decision.</summary>
   public sealed class CompzeMsSqlConnection : ICompzeMsSqlConnection
   {
      const string Description = "a SQL Server connection";

      SqlConnection Connection { get; }

      internal CompzeMsSqlConnection(string connectionString)
      {
         ProbeSqlClientConcurrency.EnsureSubscribed();
         Connection = new SqlConnection(connectionString);
      }

      public void Open()
      {
         try
         {
            Connection.Open();
         }
         catch(NotSupportedException refusal) //.NET's refusal to make the transaction distributed, which a second durable resource requires.
         {
            throw new DurableResourceAlreadyInTheTransactionException(Description, refusal);
         }
      }

      public async Task OpenAsync()
      {
         try
         {
            await Connection.OpenAsync().caf();
         }
         catch(NotSupportedException refusal) //.NET's refusal to make the transaction distributed, which a second durable resource requires.
         {
            throw new DurableResourceAlreadyInTheTransactionException(Description, refusal);
         }
      }

      public SqlCommand CreateCommand() => Connection.CreateCommand();

      public void Dispose() => Connection.Dispose();

      public ValueTask DisposeAsync() => Connection.DisposeAsync();
   }
}
