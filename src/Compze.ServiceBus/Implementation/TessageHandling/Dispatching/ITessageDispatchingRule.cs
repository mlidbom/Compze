using Compze.ServiceBus.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.ServiceBus.Implementation.Transport.Abstractions;

namespace Compze.ServiceBus.Implementation.TessageHandling.Dispatching;

interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
