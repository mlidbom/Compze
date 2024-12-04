using System;
using System.Transactions;
using JetBrains.Annotations;

namespace Compze.SystemCE.TransactionsCE;

static class TransactionScopeCe
{
   public static void SuppressAmbientAndExecuteInNewTransaction(Action action) => SuppressAmbient(() => Execute(action));

   public static void SuppressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

   public static TResult Execute<TResult>([InstantHandle] Func<TResult> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transaction = CreateScope(option);
      var result = action();
      transaction.Complete();
      return result;
   }

   public static void Execute([InstantHandle] Action action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transaction = CreateScope(option);
      action();
      transaction.Complete();
   }

   static TransactionScope CreateScope(TransactionScopeOption options) =>
      new(options,
          new TransactionOptions
          {
             IsolationLevel = IsolationLevel.Serializable
          },
          TransactionScopeAsyncFlowOption.Enabled);
}