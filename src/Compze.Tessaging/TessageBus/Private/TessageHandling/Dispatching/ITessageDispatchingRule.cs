using Compze.Tessaging.Internal.Transport;
using Compze.Tessaging.TessageBus.Private.Inbox;
using Compze.Tessaging.Private.Transport;

namespace Compze.Tessaging.TessageBus.Private.TessageHandling.Dispatching;

interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
