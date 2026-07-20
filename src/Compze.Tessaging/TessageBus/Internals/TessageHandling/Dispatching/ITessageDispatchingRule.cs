using Compze.Tessaging.Internals.Transport.Abstractions;
using Compze.Tessaging.TessageBus.Internals.TessageHandling.Inbox;

namespace Compze.Tessaging.TessageBus.Internals.TessageHandling.Dispatching;

interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
