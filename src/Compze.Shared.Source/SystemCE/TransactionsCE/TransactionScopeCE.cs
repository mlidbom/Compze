using System;
using System.Threading.Tasks;
using System.Transactions;
using Compze.Utilities.SystemCE.ActionFuncHarmonization;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;
using JetBrains.Annotations;

namespace Compze.Utilities.SystemCE.TransactionsCE;

internal static class TransactionScopeCe
{
   public static void SuppressAmbientAndExecuteInNewTransaction(Action action) => SuppressAmbient(() => Execute(action));

   public static void Execute([InstantHandle] Action action, TransactionScopeOption option = TransactionScopeOption.Required) => Execute(action.AsFunc(), option);

   public static void SuppressAmbient(Action action) => Execute(action, TransactionScopeOption.Suppress);

   public static TResult Execute<TResult>([InstantHandle] Func<TResult> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option,
                                                        new TransactionOptions { IsolationLevel = IsolationLevel.Serializable },
                                                        TransactionScopeAsyncFlowOption.Enabled);
      var result = action();
      transactionScope.Complete();
      return result;
   }


   public static async Task SuppressAmbientAsync(Func<Task> action) => await ExecuteAsync(action, TransactionScopeOption.Suppress).caf();



   public static async Task ExecuteAsync([InstantHandle] Func<Task> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option,
                                                        new TransactionOptions { IsolationLevel = IsolationLevel.Serializable },
                                                        TransactionScopeAsyncFlowOption.Enabled);
      await action().caf();
      transactionScope.Complete();
   }

   public static async Task<TResult> SuppressAmbientAsync<TResult>(Func<Task<TResult>> action) => await ExecuteAsync(action, TransactionScopeOption.Suppress).caf();



   public static async Task<TResult> ExecuteAsync<TResult>([InstantHandle] Func<Task<TResult>> action, TransactionScopeOption option = TransactionScopeOption.Required)
   {
      using var transactionScope = new TransactionScope(option,
                                                        new TransactionOptions { IsolationLevel = IsolationLevel.Serializable },
                                                        TransactionScopeAsyncFlowOption.Enabled);
      var result = await action().caf();
      transactionScope.Complete();
      return result;
   }

}
