using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Typermedia.HandlerRegistration;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Typermedia;

class InProcessTypermediaNavigator(ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeResolver scopeResolver) : IInProcessTypermediaNavigator
{
   readonly ITypermediaHandlerRegistry _typermediaHandlerRegistry = typermediaHandlerRegistry;
   readonly IScopeResolver _scopeResolver = scopeResolver;
   readonly IUsageGuard _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(typermediaHandlerRegistry));

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetTommandHandler(tommand);
      return tommandHandler.Invoke(tommand, _scopeResolver);
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetVoidTommandHandler(tommand);
      tommandHandler.Invoke(tommand, _scopeResolver);
   }

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      var tueryHandler = _typermediaHandlerRegistry.GetTueryHandler(tuery);
      return tueryHandler.Invoke(tuery, _scopeResolver);
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      TessageValidator.AssertValidToExecuteLocally(tessage);
   }
}
