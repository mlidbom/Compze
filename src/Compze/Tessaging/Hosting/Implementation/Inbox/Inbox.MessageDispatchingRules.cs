using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Hosting.Implementation;

partial class Inbox
{
   class QueriesExecuteAfterAllCommandsAndEventsAreDone : IMessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage)
      {
         if(candidateMessage.MessageTypeEnum != TransportMessage.TransportMessageType.NonTransactionalQuery) return true;

         return executing.AtMostOnceCommands.None() && executing.ExactlyOnceCommands.None() && executing.ExactlyOnceEvents.None();
      }
   }

   class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : IMessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingMessagesSnapshot executing, TransportMessage.InComing candidateMessage)
      {
         if(candidateMessage.MessageTypeEnum == TransportMessage.TransportMessageType.NonTransactionalQuery) return true;

         return executing.AtMostOnceCommands.None() && executing.ExactlyOnceCommands.None() && executing.ExactlyOnceEvents.None();
      }
   }
}