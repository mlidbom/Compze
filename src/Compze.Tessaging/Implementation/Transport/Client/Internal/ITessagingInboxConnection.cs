using System.Threading.Tasks;
using Compze.Core.Public;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface ITessagingInboxConnection
{
    TessageTypesInternal.EndpointInformation EndpointInformation { get; }
    Task SendAsync(IExactlyOnceTevent tevent);
    Task SendAsync(IExactlyOnceTommand tommand);
    void EnqueueForDelivery(TessageId tessageId, IExactlyOnceTessage tessage);
    void StartDelivery();
    void StopDelivery();
}
