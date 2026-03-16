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
      return _serviceLocator.ExecuteInIsolatedScope(() =>
      {
         var handler = _handlerRegistry.GetTueryHandler(tuery.GetType());
         return handler((ITuery<object>)tuery);
      });
   }

   public object ExecuteTommandWithResult(ITessage tommand)
   {
      this.Log().Debug($"Executing tommand with result {tommand.GetType().Name}");
      return ExecuteWithRetry(() => _serviceLocator.ExecuteTransactionInIsolatedScope(() =>
      {
         var handler = _handlerRegistry.GetTommandHandlerWithReturnValue(tommand.GetType());
         return handler((IAtMostOnceTypermediaTommand)tommand);
      }));
   }

   public void ExecuteVoidTommand(IAtMostOnceTypermediaTommand tommand)
   {
      this.Log().Debug($"Executing void tommand {tommand.GetType().Name}");
      ExecuteWithRetry<object?>(() =>
      {
         _serviceLocator.ExecuteTransactionInIsolatedScope(() =>
         {
            var handler = _handlerRegistry.GetVoidTommandHandler(tommand);
            handler(tommand);
         });
         return null;
      });
   }

   const int MaxAttempts = 5;

#pragma warning disable CA1031 // Catch-all is intentional — retry any exception, matching DefaultRetryPolicy behavior
   static TResult ExecuteWithRetry<TResult>(Func<TResult> action)
   {
      var remainingAttempts = MaxAttempts;
      while(true)
      {
         try
         {
            return action();
         }
         catch when(--remainingAttempts > 0)
         {
#pragma warning disable CS0219 // Variable is assigned but its value is never used
            int instrumentationPoint = 0;
#pragma warning restore CS0219 // Variable is assigned but its value is never used
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
