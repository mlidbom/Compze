using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Implementation.TessageHandling;

partial class Inbox
{
   class QueriesExecuteAfterAllCommandsAndEventsAreDone : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum != TransportTessage.TransportTessageType.NonTransactionalTuery) return true;

         return executing.AtMostOnceCommands.None() && executing.ExactlyOnceCommands.None() && executing.ExactlyOnceEvents.None();
      }
   }

   class CommandsAndEventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum == TransportTessage.TransportTessageType.NonTransactionalTuery) return true;

         return executing.AtMostOnceCommands.None() && executing.ExactlyOnceCommands.None() && executing.ExactlyOnceEvents.None();
      }
   }
}