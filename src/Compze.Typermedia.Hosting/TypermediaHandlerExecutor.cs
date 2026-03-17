using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Logging;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia.Hosting;

public class TypermediaHandlerExecutor(IServiceLocator serviceLocator, ITypermediaHandlerRegistry handlerRegistry)
{
   readonly IServiceLocator _serviceLocator = serviceLocator;
   readonly ITypermediaHandlerRegistry _handlerRegistry = handlerRegistry;

   public object ExecuteTuery(ITessage tuery)
   {
      this.Log().Debug($"Executing tuery {tuery.GetType().Name}");
      return _serviceLocator.ExecuteInIsolatedScope(scopeResolver =>
      {
         var handler = _handlerRegistry.GetTueryHandler(tuery.GetType());
         return handler((ITuery<object>)tuery, scopeResolver);
      });
   }

   public object ExecuteTommandWithResult(ITessage tommand)
   {
      this.Log().Debug($"Executing tommand with result {tommand.GetType().Name}");
      return ExecuteWithRetry(() => _serviceLocator.ExecuteTransactionInIsolatedScope(scopeResolver =>
      {
         var handler = _handlerRegistry.GetTommandHandlerWithReturnValue(tommand.GetType());
         return handler((IAtMostOnceTypermediaTommand)tommand, scopeResolver);
      }));
   }

   public void ExecuteVoidTommand(IAtMostOnceTypermediaTommand tommand)
   {
      this.Log().Debug($"Executing void tommand {tommand.GetType().Name}");
      ExecuteWithRetry<object?>(() =>
      {
         _serviceLocator.ExecuteTransactionInIsolatedScope(scopeResolver =>
         {
            var handler = _handlerRegistry.GetVoidTommandHandler(tommand);
            handler(tommand, scopeResolver);
         });
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
         //Todo: Should we have some sort of retryable exception test here perhaps? And/or a backoff delay? Rather than hammering away instantly regardless of the error?
         catch(Exception ex) when(--remainingAttempts > 0)
         {
            this.Log().Warning(ex, "Command execution failed. Retrying.");
         }
      }
   }
#pragma warning restore CA1031

   public static void RegisterWith(IComponentRegistrar registrar) =>
      registrar.Register(
         Singleton.For<TypermediaHandlerExecutor>()
                  .CreatedBy((IServiceLocator serviceLocator, ITypermediaHandlerRegistry handlerRegistry)
                                => new TypermediaHandlerExecutor(serviceLocator, handlerRegistry)));
}
