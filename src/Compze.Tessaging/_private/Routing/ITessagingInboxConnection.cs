using Compze.Tessaging._internal.Transport.Advertisement;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging._private.Routing;

///<summary>A connection through which tessages are delivered to one remote endpoint's transport server. It carries one ordered<br/>
/// stream per delivery tier the endpoint wires: the best-effort stream — draining the peer's in-memory queue in the endpoint's<br/>
/// best-effort delivery wiring, which is enqueued into directly and outlives the connection — always, and the exactly-once<br/>
/// stream, enqueued through this interface, when the outbox is wired — see<br/>
/// <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>.</summary>
interface ITessagingInboxConnection
{
    EndpointInformation EndpointInformation { get; }

    ///<summary>Queues <paramref name="tessage"/> on the connection's exactly-once stream: backed by the outbox's storage,<br/>
    /// ordered by <paramref name="deliveryStreamSequenceNumber"/> — the tessage's place in this pair's delivery stream, assigned<br/>
    /// by the outbox save — and head-of-line retried until delivered and acknowledged, the backlog surviving restarts. Only the<br/>
    /// outbox sends exactly-once, so on an endpoint without one — whose connections carry no exactly-once stream — nothing calls this.<br/>
    /// <paramref name="dedupId"/> is the envelope identity the receiving endpoint's inbox dedups on — the tessage's own <see cref="ITessageWithIdentity.Id"/>.</summary>
    void EnqueueForExactlyOnceDelivery(ITessage tessage, TessageId dedupId, long deliveryStreamSequenceNumber);
}
