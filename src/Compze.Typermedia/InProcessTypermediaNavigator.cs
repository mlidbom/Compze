using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Typermedia.HandlerRegistration;
using Compze.Abstractions.Tessaging.Validation;

namespace Compze.Typermedia;

class InProcessTypermediaNavigator(ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeServiceLocator scopeServiceLocator) : IInProcessTypermediaNavigator
{
   readonly ITypermediaHandlerRegistry _typermediaHandlerRegistry = typermediaHandlerRegistry;
   readonly IScopeServiceLocator _scopeServiceLocator = scopeServiceLocator;
   readonly IUsageGuard _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(typermediaHandlerRegistry));

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetTommandHandler(tommand);
      return tommandHandler.Invoke(tommand, _scopeServiceLocator);
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetVoidTommandHandler(tommand);
      tommandHandler.Invoke(tommand, _scopeServiceLocator);
   }

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      var tueryHandler = _typermediaHandlerRegistry.GetTueryHandler(tuery);
      return tueryHandler.Invoke(tuery, _scopeServiceLocator);
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      TessageValidator.AssertValidToExecuteLocally(tessage);
   }
}
