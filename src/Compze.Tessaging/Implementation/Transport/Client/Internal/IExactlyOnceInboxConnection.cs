using System.Threading.Tasks;
using Compze.Core.Tessaging.Public;
using Compze.Tessaging.Implementation.Abstractions;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

public interface IExactlyOnceInboxConnection
{
    TessageTypesInternal.EndpointInformation EndpointInformation { get; }
    Task SendAsync(IExactlyOnceTevent tevent);
    Task SendAsync(IExactlyOnceTommand tommand);
}
