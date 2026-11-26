using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.Engine._private;
using Compze.Tessaging.Engine.Exceptions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Typermedia._private;

///<summary>The typermedia transport server's execution site: executes an arriving remote tuery or typermedia tommand through the<br/>
/// engine's one executor (<see cref="TessageHandlerExecutor"/>), adding the arrival-side retry a remote tommand gets — a<br/>
/// tommand handler that throws is run again here, at the receiver, invisibly to the caller. A missing handler is never retried:<br/>
/// <see cref="NoHandlerException"/> is a programming error, not a transient failure.</summary>
class TypermediaHandlerExecutor
{
   readonly TessageHandlerExecutor _executor;
   readonly IScopeFactory _scopeFactory;

   TypermediaHandlerExecutor(TessageHandlerExecutor executor, IScopeFactory scopeFactory)
   {
      _executor = executor;
      _scopeFactory = scopeFactory;
   }

   public async Task<object> ExecuteTueryAsync(ITessage tuery)
   {
      this.Log().Debug($"Executing tuery {tuery.GetType().Name}");
      return await _executor.ExecuteTueryHandlerInIsolatedScopeAsync((ITuery)tuery).caf();
   }

   public async Task<object> ExecuteTommandWithResultAsync(ITessage tommand)
   {
      this.Log().Debug($"Executing tommand with result {tommand.GetType().Name}");
      return await ExecuteInOwnUnitOfWorkRetryingTheHandlerAsync(async unitOfWork => await _executor.ExecuteTommandHandlerWithResultAsync((IAtMostOnceTypermediaTommand)tommand, unitOfWork).caf()).caf();
   }

   public async Task ExecuteVoidTommandAsync(IAtMostOnceTypermediaTommand tommand)
   {
      this.Log().Debug($"Executing void tommand {tommand.GetType().Name}");
      await ExecuteInOwnUnitOfWorkRetryingTheHandlerAsync<object?>(async unitOfWork =>
      {
         await _executor.ExecuteTommandHandlerAsync(tommand, unitOfWork).caf();
         return null;
      }).caf();
   }

   const int MaxAttempts = 5;

   ///<summary>Runs <paramref name="handler"/> as its own unit of work, and when it throws, runs it again in a new one, up to<br/>
   /// <see cref="MaxAttempts"/> times in all.</summary>
   ///<remarks>A failure after the handler has returned is never retried: it comes from the unit of work's commit, or after it,<br/>
   /// and a commit that the database made can still fail at the caller, such as when the connection drops before the<br/>
   /// acknowledgment arrives. Running the handler again then would commit its effects a second time, and a tommand's<br/>
   /// transactional effects commit at most once for each post.</remarks>
#pragma warning disable CA1031 // Catch-all is intentional — retry any exception the handler throws, matching DefaultRetryPolicy behavior
   async Task<TResult> ExecuteInOwnUnitOfWorkRetryingTheHandlerAsync<TResult>(Func<IUnitOfWorkResolver, Task<TResult>> handler)
   {
      var remainingAttempts = MaxAttempts;
      while(true)
      {
         var handlerReturned = false;
         try
         {
            return await _scopeFactory.ExecuteUnitOfWorkAsync(async unitOfWork =>
            {
               var result = await handler(unitOfWork).caf();
               handlerReturned = true;
               return result;
            }).caf();
         }
         //Todo:review: Should we have some sort of retryable exception test here perhaps? And/or a backoff delay? Rather than hammering away instantly regardless of the error?
         catch(Exception exception) when(!handlerReturned && exception is not NoHandlerException && --remainingAttempts > 0) //A missing handler is a programming error, not a transient failure to retry.
         {
            this.Log().Warning(exception, "Tommand handler failed. Retrying.");
         }
      }
   }
#pragma warning restore CA1031

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<TypermediaHandlerExecutor>()
                  .CreatedBy((TessageHandlerExecutor executor, IScopeFactory scopeFactory) => new TypermediaHandlerExecutor(executor, scopeFactory)));
}
