using Compze.Tessaging.Internals.Transport.Abstractions;
using Compze.Tessaging.TessageBus.Internals.TessageHandling.Dispatching;

namespace Compze.Tessaging.TessageBus.Internals.TessageHandling.Inbox;

public partial class Inbox
{
   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage) =>
         executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
   }
}