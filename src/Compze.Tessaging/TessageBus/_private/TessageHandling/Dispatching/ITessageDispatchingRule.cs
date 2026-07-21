using Compze.Tessaging.TessageBus._private.Inbox;
using Compze.Tessaging._private.Transport;

namespace Compze.Tessaging.TessageBus._private.TessageHandling.Dispatching;

interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
