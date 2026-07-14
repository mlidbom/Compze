using Compze.Abstractions.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.Transport;

namespace Compze.Tessaging.Implementation.Transport.Client.Internal;

///<summary>A connection through which tessages are delivered to one remote endpoint's transport server. It carries two ordered<br/>
/// streams, one per delivery guarantee — see <c>src/Compze.Tessaging/_docs/tevent-delivery-model.md</c>.</summary>
interface ITessagingInboxConnection
{
    EndpointInformation EndpointInformation { get; }

    ///<summary>Queues <paramref name="tessage"/> on the connection's exactly-once stream: backed by the outbox's storage,<br/>
    /// head-of-line retried until delivered and acknowledged, the backlog surviving restarts in send order.<br/>
    /// <paramref name="dedupId"/> is the envelope identity the receiving endpoint's inbox dedups on — the tessage's own <see cref="IAtMostOnceTessage.Id"/>.</summary>
    void EnqueueForExactlyOnceDelivery(ITessage tessage, TessageId dedupId);

    ///<summary>Queues <paramref name="tessage"/> on the connection's transient stream: in-memory, best-effort, delivered in order<br/>
    /// while deliveries succeed. A delivery failure drops the remaining queued stream whole — the unit of loss is the stream, never a<br/>
    /// single tessage mid-stream — and tessages queued afterwards form a new live stream.<br/>
    /// <paramref name="envelopeId"/> is a fresh identity minted per publish, shared by every subscriber's delivery of the same tevent:<br/>
    /// it carries no dedup meaning on this leg (nothing is ever re-sent) and exists so in-flight tracking can correlate the fan-out.</summary>
    void EnqueueForTransientDelivery(ITessage tessage, TessageId envelopeId);
}
