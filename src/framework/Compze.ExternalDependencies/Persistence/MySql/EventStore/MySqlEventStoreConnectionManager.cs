using System;
using System.Transactions;
using Compze.Persistence.MySql.SystemExtensions;
using JetBrains.Annotations;
using MySql.Data.MySqlClient;

namespace Compze.Persistence.MySql.EventStore;

class MySqlEventStoreConnectionManager(IMySqlConnectionPool sqlConnectionPool)
{
   readonly IMySqlConnectionPool _connectionPool = sqlConnectionPool;

   public void UseConnection([InstantHandle] Action<ICompzeMySqlConnection> action)
   {
      AssertTransactionPolicy(false);
      _connectionPool.UseConnection(action);
   }

   public void UseCommand([InstantHandle]Action<MySqlCommand> action) => UseCommand(false, action);
   public void UseCommand(bool suppressTransactionWarning, [InstantHandle] Action<MySqlCommand> action)
   {
      AssertTransactionPolicy(suppressTransactionWarning);
      _connectionPool.UseCommand(action);
   }

   public TResult UseCommand<TResult>([InstantHandle]Func<MySqlCommand, TResult> action) => UseCommand(false, action);
   public TResult UseCommand<TResult>(bool suppressTransactionWarning, [InstantHandle] Func<MySqlCommand, TResult> action)
   {
      AssertTransactionPolicy(suppressTransactionWarning);
      return _connectionPool.UseCommand(action);
   }

   // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
   static void AssertTransactionPolicy(bool suppressTransactionWarning)
   {
      if (!suppressTransactionWarning && Transaction.Current == null)
      {
         throw new Exception("You must use a transaction to make modifications to the event store.");
      }
   }
}