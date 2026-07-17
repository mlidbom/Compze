using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Tessaging.Typermedia.HandlerRegistration;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Typermedia;

class LocalTypermediaNavigatorSession(ITypermediaHandlerRegistry typermediaHandlerRegistry, IScopeResolver scopeResolver) : ILocalTypermediaNavigatorSession
{
   readonly ITypermediaHandlerRegistry _typermediaHandlerRegistry = typermediaHandlerRegistry;
   readonly IScopeResolver _scopeResolver = scopeResolver;
   readonly IUsageGuard _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(typermediaHandlerRegistry));

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetTommandHandler(tommand);
      //CommonAssertion has asserted the caller's ambient transaction (an IStrictlyLocalTommand must be sent transactionally), so the caller's scope IS a unit of work - From certifies exactly that.
      return tommandHandler.Invoke(tommand, UnitOfWorkResolver.From(_scopeResolver));
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetVoidTommandHandler(tommand);
      tommandHandler.Invoke(tommand, UnitOfWorkResolver.From(_scopeResolver));
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
