using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

internal interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
