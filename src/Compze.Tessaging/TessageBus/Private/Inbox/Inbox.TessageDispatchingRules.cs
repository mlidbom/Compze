using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageBus.Private.TessageHandling.Dispatching;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.TessageBus.Private.Inbox;

partial class Inbox
{
   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage) =>
         executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
   }
}