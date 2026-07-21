using Compze.Tessaging._internal.Transport;
using Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.Inbox;

partial class Inbox
{
   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage) =>
         executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
   }
}