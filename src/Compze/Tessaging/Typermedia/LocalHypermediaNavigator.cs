using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading;

namespace Compze.Tessaging.Typermedia;

static class LocalHypermediaNavigatorRegistrar
{
   internal static IComponentRegistrar InProcessHypermediaNavigator(this IComponentRegistrar registrar)
      => registrar.Register(Typermedia.InProcessHypermediaNavigator.RegisterWith);
}

class InProcessHypermediaNavigator : IInProcessHypermediaNavigator
{
   internal static void RegisterWith(IComponentRegistrar registrar)
      => registrar.Register(Scoped.For<IInProcessHypermediaNavigator>()
                                  .CreatedBy((ITessageHandlerRegistry tessageHandlerRegistry)
                                                => new InProcessHypermediaNavigator(tessageHandlerRegistry)));

   readonly ITessageHandlerRegistry _handlerRegistry;
   readonly IUsageGuard _contextGuard;

   public InProcessHypermediaNavigator(ITessageHandlerRegistry handlerRegistry)
   {
      _handlerRegistry = handlerRegistry;
      _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard(this));
   }

   public TResult Execute<TResult>(IStrictlyLocalTommand<TResult> tommand)
   {
      CommonAssertion(tommand);

      var commandHandler = _handlerRegistry.GetCommandHandler(tommand);
      return commandHandler.Invoke(tommand);
   }

   public void Execute(IStrictlyLocalTommand tommand)
   {
      CommonAssertion(tommand);

      var commandHandler = _handlerRegistry.GetCommandHandler(tommand);
      commandHandler.Invoke(tommand);
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
