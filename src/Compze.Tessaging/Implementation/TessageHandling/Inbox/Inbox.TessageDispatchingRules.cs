using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   //todo:performance: this is a working first draft, but it will hamper parallelism. What we actually need is to ensure that tueries sent from an endpoint are executed after any tommands or tevents sent before them from that endpoint.
   public class TueriesExecuteAfterAllTommandsAndTeventsAreDone : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum != TransportTessageType.TyperMediaTuery) return true;

         return executing.AtMostOnceTommands.None() && executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }

   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum == TransportTessageType.TyperMediaTuery) return true;

         return executing.AtMostOnceTommands.None() && executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }
}