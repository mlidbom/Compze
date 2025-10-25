using Compze.Tessaging.Implementation.TessageHandling.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

interface ITessageDispatchingRule
{
   bool CanBeDispatched(IExecutingTessagesSnapshot executing, TransportTessage.InComing candidateTessage);
}
