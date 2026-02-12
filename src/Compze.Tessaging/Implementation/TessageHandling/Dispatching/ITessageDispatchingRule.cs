using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

public interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
