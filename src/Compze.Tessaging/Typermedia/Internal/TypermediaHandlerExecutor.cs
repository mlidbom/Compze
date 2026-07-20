using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine;
using Compze.Tessaging.Engine.Internal;
using Compze.Tessaging.Engine.Exceptions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia.Internal;

///<summary>The typermedia transport server's execution site: executes an arriving remote tuery or typermedia tommand through the<br/>
/// engine's one executor (<see cref="TessageHandlerExecutor"/>), adding the arrival-side retry a remote tommand gets — a<br/>
/// transient handling failure is retried here, at the receiver, invisibly to the caller. A missing handler is never retried:<br/>
/// <see cref="NoHandlerException"/> is a programming error, not a transient failure.</summary>
class TypermediaHandlerExecutor
{
   readonly TessageHandlerExecutor _executor;

   TypermediaHandlerExecutor(TessageHandlerExecutor executor) => _executor = executor;

   public async Task<object> ExecuteTueryAsync(ITessage tuery)
   {
      this.Log().Debug($"Executing tuery {tuery.GetType().Name}");
      return await _executor.ExecuteTueryHandlerInIsolatedScopeAsync((ITuery)tuery).caf();
   }

   public async Task<object> ExecuteTommandWithResultAsync(ITessage tommand)
   {
      this.Log().Debug($"Executing tommand with result {tommand.GetType().Name}");
      return await ExecuteWithRetryAsync(async () => await _executor.ExecuteTommandHandlerWithResultInOwnUnitOfWorkAsync((IAtMostOnceTypermediaTommand)tommand).caf()).caf();
   }

   public async Task ExecuteVoidTommandAsync(IAtMostOnceTypermediaTommand tommand)
   {
      this.Log().Debug($"Executing void tommand {tommand.GetType().Name}");
      await ExecuteWithRetryAsync<object?>(async () =>
      {
         await _executor.ExecuteVoidTommandHandlerInOwnUnitOfWorkAsync(tommand).caf();
         return null;
      }).caf();
   }

   const int MaxAttempts = 5;

#pragma warning disable CA1031 // Catch-all is intentional — retry any exception, matching DefaultRetryPolicy behavior
   async Task<TResult> ExecuteWithRetryAsync<TResult>(Func<Task<TResult>> action)
   {
      var remainingAttempts = MaxAttempts;
      while(true)
      {
         try
         {
            return await action().caf();
         }
         //Todo:review: Should we have some sort of retryable exception test here perhaps? And/or a backoff delay? Rather than hammering away instantly regardless of the error?
         catch(Exception ex) when(ex is not NoHandlerException && --remainingAttempts > 0) //A missing handler is a programming error, not a transient failure to retry.
         {
            this.Log().Warning(ex, "Command execution failed. Retrying.");
         }
      }
   }
#pragma warning restore CA1031

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<TypermediaHandlerExecutor>()
                  .CreatedBy((TessageHandlerExecutor executor) => new TypermediaHandlerExecutor(executor)));
}
