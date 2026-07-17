using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Tessaging.Engine;

namespace Compze.Tessaging.Typermedia.Hosting;

///<summary>The typermedia transport server's execution site: executes an arriving remote tuery or typermedia tommand through the<br/>
/// engine's one executor (<see cref="TessageHandlerExecutor"/>), adding the arrival-side retry a remote tommand gets — a<br/>
/// transient handling failure is retried here, at the receiver, invisibly to the caller. A missing handler is never retried:<br/>
/// <see cref="NoHandlerException"/> is a programming error, not a transient failure.</summary>
public class TypermediaHandlerExecutor
{
   readonly TessageHandlerExecutor _executor;

   internal TypermediaHandlerExecutor(TessageHandlerExecutor executor) => _executor = executor;

   public object ExecuteTuery(ITessage tuery)
   {
      this.Log().Debug($"Executing tuery {tuery.GetType().Name}");
      return _executor.ExecuteTueryHandlerInIsolatedScope((ITuery)tuery);
   }

   public object ExecuteTommandWithResult(ITessage tommand)
   {
      this.Log().Debug($"Executing tommand with result {tommand.GetType().Name}");
      return ExecuteWithRetry(() => _executor.ExecuteTommandHandlerWithResultInOwnUnitOfWork((IAtMostOnceTypermediaTommand)tommand));
   }

   public void ExecuteVoidTommand(IAtMostOnceTypermediaTommand tommand)
   {
      this.Log().Debug($"Executing void tommand {tommand.GetType().Name}");
      ExecuteWithRetry<object?>(() =>
      {
         _executor.ExecuteVoidTommandHandlerInOwnUnitOfWork(tommand);
         return null;
      });
   }

   const int MaxAttempts = 5;

#pragma warning disable CA1031 // Catch-all is intentional — retry any exception, matching DefaultRetryPolicy behavior
   TResult ExecuteWithRetry<TResult>(Func<TResult> action)
   {
      var remainingAttempts = MaxAttempts;
      while(true)
      {
         try
         {
            return action();
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
