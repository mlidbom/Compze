using Compze.Tessaging.Internal.Transport.Abstractions;
using Compze.Tessaging.TessageBus.Internal.TessageHandling.Dispatching;

namespace Compze.Tessaging.TessageBus.Internal.Inbox;

public partial class Inbox
{
   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage) =>
         executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
   }
}