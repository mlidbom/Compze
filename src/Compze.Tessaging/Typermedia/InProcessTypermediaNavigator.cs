using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Typermedia.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.UsageGuards;

namespace Compze.Tessaging.Typermedia;

public static class LocalHypermediaNavigatorRegistrar
{
   public static IComponentRegistrar InProcessHypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Typermedia.InProcessTypermediaNavigator.RegisterWith);
}

public class InProcessTypermediaNavigator : IInProcessTypermediaNavigator
{
   public static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IInProcessTypermediaNavigator>()
                                  .CreatedBy((ITessageHandlerRegistry tessageHandlerRegistry)
                                                => new InProcessTypermediaNavigator(tessageHandlerRegistry)));

   readonly ITessageHandlerRegistry _handlerRegistry;
   readonly IUsageGuard _contextGuard;

   public InProcessTypermediaNavigator(ITessageHandlerRegistry handlerRegistry)
   {
      _handlerRegistry = handlerRegistry;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _handlerRegistry.GetTommandHandler(tommand);
      return tommandHandler.Invoke(tommand);
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var tommandHandler = _handlerRegistry.GetTommandHandler(tommand);
      tommandHandler.Invoke(tommand);
   }

   public TResult Execute<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      var tueryHandler = _handlerRegistry.GetTueryHandler(tuery);
      return tueryHandler.Invoke(tuery);
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      TessageInspector.AssertValidToExecuteLocally(tessage);
   }
}
