using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface ITessagingInboxConnection
{
    TessageTypesInternal.EndpointInformation EndpointInformation { get; }
    void EnqueueForDelivery(TessageId tessageId, IExactlyOnceTessage tessage);
    void StartDelivery();
    void StopDelivery();
}
