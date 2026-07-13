using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITessagingInboxConnection
{
    EndpointInformation EndpointInformation { get; }
    void EnqueueForDelivery(ITessage tessage, TessageId dedupId);
}
