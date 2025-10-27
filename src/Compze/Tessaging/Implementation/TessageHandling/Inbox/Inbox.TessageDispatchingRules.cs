using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Utilities.SystemCE.LinqCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

partial class Inbox
{
   class QueriesExecuteAfterAllTommandsAndTeventsAreDone : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum != TransportTessageType.TyperMediaTuery) return true;

         return executing.AtMostOnceTommands.None() && executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }

   class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum == TransportTessageType.TyperMediaTuery) return true;

         return executing.AtMostOnceTommands.None() && executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }
}