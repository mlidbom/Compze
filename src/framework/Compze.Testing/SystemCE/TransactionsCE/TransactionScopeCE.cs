using System;
using System.Transactions;
using JetBrains.Annotations;

namespace Compze.Testing.SystemCE.TransactionsCE;

static class TransactionScopeCe
{
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