using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

interface ITessagingInboxConnection
{
    TessageTypesInternal.EndpointInformation EndpointInformation { get; }
    void EnqueueForDelivery(IExactlyOnceTessage tessage);
    void StartDelivery();
    void StopDelivery();
}
