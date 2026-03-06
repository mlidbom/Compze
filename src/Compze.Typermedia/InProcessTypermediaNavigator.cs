using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Typermedia.HandlerRegistration;
using Compze.Typermedia.Validation;

namespace Compze.Typermedia;

class InProcessTypermediaNavigator(ITypermediaHandlerRegistry typermediaHandlerRegistry) : IInProcessTypermediaNavigator
{
   readonly ITypermediaHandlerRegistry _typermediaHandlerRegistry = typermediaHandlerRegistry;
   readonly IUsageGuard _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(typermediaHandlerRegistry));

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetTommandHandler(tommand);
      return tommandHandler.Invoke(tommand);
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetVoidTommandHandler(tommand);
      tommandHandler.Invoke(tommand);
   }

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      var tueryHandler = _typermediaHandlerRegistry.GetTueryHandler(tuery);
      return tueryHandler.Invoke(tuery);
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      TypermediaValidator.AssertValidToExecuteLocally(tessage);
   }
}
