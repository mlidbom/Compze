using System;
using System.Transactions;
using JetBrains.Annotations;

namespace Compze.SystemCE.TransactionsCE;

public static class TransactionScopeCe
{
   public static void SuppressAmbientAndExecuteInNewTransaction(Action action) => SuppressAmbient(() => Execute(action));

   public static TResult SuppressAmbientAndExecuteInNewTransaction<TResult>([InstantHandle] Func<TResult> action) => SuppressAmbient(() => Execute(action));

   public static void SuppressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

   public static TResult SuppressAmbient<TResult>([InstantHandle] Func<TResult> action) => Execute(action, TransactionScopeOption.Suppress);

   public static TResult Execute<TResult>([InstantHandle] Func<TResult> action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
   {
      using var transaction = CreateScope(option, isolationLevel);
      var result = action();
      transaction.Complete();
      return result;
   }

   public static void Execute([InstantHandle] Action action, TransactionScopeOption option = TransactionScopeOption.Required, IsolationLevel isolationLevel = IsolationLevel.Serializable)
   {
      using var transaction = CreateScope(option, isolationLevel);
      action();
      transaction.Complete();
   }

   static TransactionScope CreateScope(TransactionScopeOption options, IsolationLevel isolationLevel) =>
      new(options,
          new TransactionOptions
          {
             IsolationLevel = isolationLevel
          },
          TransactionScopeAsyncFlowOption.Enabled);
}