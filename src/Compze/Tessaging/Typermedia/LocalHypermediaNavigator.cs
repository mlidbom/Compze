using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Typermedia.Public;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
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
                                  .CreatedBy((IMessageHandlerRegistry messageHandlerRegistry)
                                                => new InProcessHypermediaNavigator(messageHandlerRegistry)));

   readonly IMessageHandlerRegistry _handlerRegistry;
   readonly IUsageGuard _contextGuard;

   public InProcessHypermediaNavigator(IMessageHandlerRegistry handlerRegistry)
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

   public TResult Execute<TQuery, TResult>(IStrictlyLocalTuery<TQuery, TResult> tuery) where TQuery : IStrictlyLocalTuery<TQuery, TResult>
   {
      CommonAssertion(tuery);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(tuery is ICreateMyOwnResultTuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      var queryHandler = _handlerRegistry.GetQueryHandler(tuery);
      return queryHandler.Invoke(tuery);
   }

   void CommonAssertion(ITessage tessage)
   {
      _contextGuard.EnsureAccessValid();
      MessageInspector.AssertValidToExecuteLocally(tessage);
   }
}
