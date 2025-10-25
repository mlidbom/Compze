using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Implementation.TessageHandling;

partial class Inbox
{
   class QueriesExecuteAfterAllTommandsAndTeventsAreDone : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum != TransportTessage.TransportTessageType.NonTransactionalTuery) return true;

         return executing.AtMostOnceTommands.None() && executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }

   class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum == TransportTessage.TransportTessageType.NonTransactionalTuery) return true;

         return executing.AtMostOnceTommands.None() && executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }
}