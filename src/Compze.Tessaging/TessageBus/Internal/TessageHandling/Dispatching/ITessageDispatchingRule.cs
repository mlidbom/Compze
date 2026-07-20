using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageBus.Internal.Inbox;

namespace Compze.Tessaging.TessageBus.Internal.TessageHandling.Dispatching;

interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
