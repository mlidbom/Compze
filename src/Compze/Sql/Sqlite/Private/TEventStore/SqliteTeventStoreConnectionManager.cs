using System;
using System.Transactions;
using JetBrains.Annotations;
using Microsoft.Data.Sqlite;

namespace Compze.Sql.Sqlite.Private.TEventStore;

class SqliteTeventStoreConnectionManager(ISqliteConnectionPool sqlConnectionPool)
{
   readonly ISqliteConnectionPool _connectionPool = sqlConnectionPool;

   public void UseConnection([InstantHandle] Action<ICompzeSqliteConnection> action)
   {
      AssertTransactionPolicy(false);
      _connectionPool.UseConnection(action);
   }

   public void UseCommand([InstantHandle]Action<SqliteCommand> action) => UseCommand(false, action);
   public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<SqliteCommand> action)
   {
      AssertTransactionPolicy(suppressTransactionWarning);
      _connectionPool.UseCommand(action);
   }

   public TResult UseCommand<TResult>([InstantHandle]Func<SqliteCommand, TResult> action) => UseCommand(false, action);
   public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<SqliteCommand, TResult> action)
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
