using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage)
      {
         if(candidateTessage.TessageTypeEnum == TransportTessageType.TyperMediaTuery) return true;

         return executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
      }
   }
}