using Compze.Tessaging.Implementation.Transport.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

///<summary>The receiving half of the endpoint's exactly-once Tessaging pipeline: arriving tessages are registered here by the<br/>
/// endpoint's transport request handling, persisted and deduped, then handled transactionally — retried until handled.</summary>
public interface IInbox
{
   Task StartAsync();

   Task ReceiveAsync(TransportTessage.InComing tessage);
}
