﻿using Compze.Messaging.Buses.Implementation;
using Compze.SystemCE.ThreadingCE;

namespace Compze.Messaging.Hypermedia;

class LocalHypermediaNavigator(IMessageHandlerRegistry handlerRegistry) : ILocalHypermediaNavigator
{
   readonly IMessageHandlerRegistry _handlerRegistry = handlerRegistry;
   readonly ISingleContextUseGuard _contextGuard = new CombinationUsageGuard(new SingleTransactionUsageGuard());

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
      _contextGuard.AssertNoContextChangeOccurred(this);
      MessageInspector.AssertValidToExecuteLocally(message);
   }
}