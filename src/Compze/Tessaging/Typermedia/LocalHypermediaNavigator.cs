using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Common;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.Threading;

namespace Compze.Tessaging.Typermedia;

static class LocalHypermediaNavigatorRegistrar
{
   internal static IDependencyRegistrar InProcessHypermediaNavigator(this IDependencyRegistrar registrar)
      => registrar.Register(Typermedia.InProcessHypermediaNavigator.RegisterWith);
}

class InProcessHypermediaNavigator : IInProcessHypermediaNavigator
{
   internal static void RegisterWith(IDependencyRegistrar registrar)
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

   public TResult Execute<TResult>(IStrictlyLocalCommand<TResult> command)
   {
      CommonAssertion(command);

      var commandHandler = _handlerRegistry.GetCommandHandler(command);
      return commandHandler.Invoke(command);
   }

   public void Execute(IStrictlyLocalCommand command)
   {
      CommonAssertion(command);

      var commandHandler = _handlerRegistry.GetCommandHandler(command);
      commandHandler.Invoke(command);
   }

   public TResult Execute<TQuery, TResult>(IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : IStrictlyLocalQuery<TQuery, TResult>
   {
      CommonAssertion(query);

      // ReSharper disable once SuspiciousTypeConversion.Global
      //Todo: Test and stop disabling ReSharper warning
      if(query is ICreateMyOwnResultQuery<TResult> selfCreating)
         return selfCreating.CreateResult();

      var queryHandler = _handlerRegistry.GetQueryHandler(query);
      return queryHandler.Invoke(query);
   }

   void CommonAssertion(IMessage message)
   {
      _contextGuard.EnsureAccessValid();
      MessageInspector.AssertValidToExecuteLocally(message);
   }
}
