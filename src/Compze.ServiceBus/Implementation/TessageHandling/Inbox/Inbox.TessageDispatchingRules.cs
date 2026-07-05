using Compze.ServiceBus.Implementation.TessageHandling.Abstractions;
using Compze.ServiceBus.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.ServiceBus.Implementation.Transport.Abstractions;

namespace Compze.ServiceBus.Implementation.TessageHandling.Inbox;

public partial class Inbox
{
   public class TommandsAndTeventHandlersDoNotRunInParallelWithEachOtherInTheSameEndpoint : ITessageDispatchingRule
   {
      public bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage) =>
         executing.ExactlyOnceTommands.None() && executing.ExactlyOnceTevents.None();
   }
}