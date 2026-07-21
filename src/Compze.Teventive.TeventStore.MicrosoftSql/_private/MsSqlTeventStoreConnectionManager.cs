using System.Transactions;
using Compze.Sql.MicrosoftSql;
using JetBrains.Annotations;
using Microsoft.Data.SqlClient;

namespace Compze.Teventive.TeventStore.MicrosoftSql._private;

class MsSqlTeventStoreConnectionManager(IMsSqlConnectionPool sqlConnectionPool)
{
   readonly IMsSqlConnectionPool _connectionPool = sqlConnectionPool;

   public void UseConnection([InstantHandle] Action<ICompzeMsSqlConnection> action)
   {
      AssertTransactionPolicy(false);
      _connectionPool.UseConnection(action);
   }

   public void UseCommand([InstantHandle]Action<SqlCommand> action) => UseCommand(false, action);

   void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<SqlCommand> action)
   {
      AssertTransactionPolicy(suppressTransactionWarning);
      _connectionPool.UseCommand(action);
   }

   public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<SqlCommand, TResult> action)
   {
      AssertTransactionPolicy(suppressTransactionWarning);
      return _connectionPool.UseCommand(action);
   }

   // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
   static void AssertTransactionPolicy(bool suppressTransactionWarning)
   {
      if (!suppressTransactionWarning && Transaction.Current == null)
      {
         throw new Exception("You must use a transaction to make modifications to the tevent store.");
      }
   }
}
