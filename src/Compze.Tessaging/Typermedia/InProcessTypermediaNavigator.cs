using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.UsageGuards;
using Compze.Typermedia;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Tessaging.Typermedia;

static class LocalHypermediaNavigatorRegistrar
{
   public static IComponentRegistrar InProcessHypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(InProcessTypermediaNavigator.RegisterWith);
}

class InProcessTypermediaNavigator : IInProcessTypermediaNavigator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IInProcessTypermediaNavigator>()
                                  .CreatedBy((ITessageHandlerRegistry tessagingHandlerRegistry, ITypermediaHandlerRegistry typermediaHandlerRegistry)
                                                => new InProcessTypermediaNavigator(tessagingHandlerRegistry, typermediaHandlerRegistry)));

   readonly ITessageHandlerRegistry _tessagingHandlerRegistry;
   readonly ITypermediaHandlerRegistry _typermediaHandlerRegistry;
   readonly IUsageGuard _contextGuard;

   InProcessTypermediaNavigator(ITessageHandlerRegistry tessagingHandlerRegistry, ITypermediaHandlerRegistry typermediaHandlerRegistry)
   {
      _tessagingHandlerRegistry = tessagingHandlerRegistry;
      _typermediaHandlerRegistry = typermediaHandlerRegistry;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _typermediaHandlerRegistry.GetTommandHandler(tommand);
      return tommandHandler.Invoke(tommand);
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _tessagingHandlerRegistry.GetTommandHandler(tommand);
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
      TessageInspector.AssertValidToExecuteLocally(tessage);
   }
}
