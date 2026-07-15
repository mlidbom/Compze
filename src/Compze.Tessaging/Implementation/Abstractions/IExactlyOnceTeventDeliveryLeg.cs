using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

///<summary>The exactly-once remote delivery leg for tevents: durable, deduped, retried-until-handled delivery to subscribers on<br/>
/// other endpoints. Wiring supplies the delivery legs — exactly-once Tessaging wires this one (the <see cref="IOutbox"/> is it) on<br/>
/// top of the transport-speaking core that wires its best-effort sibling <see cref="ITransientTeventDeliveryLeg"/>, and the<br/>
/// <see cref="ITeventPublisher"/> routes every published <see cref="IExactlyOnceTevent"/> through it. An endpoint that wires no<br/>
/// remote delivery is a deliberately in-process composition: its tevents are delivered by participation alone.</summary>
interface IExactlyOnceTeventDeliveryLeg
{
   ///<summary>Publishes the wrapped tevent to every remote subscriber - the whole wrapper travels the wire, so publisher identity crosses endpoints with zero information loss.<br/>
   /// The wrapper carries only publisher identity, not delivery-guarantee markers; the dedup identity is the wrapped tevent's own <see cref="ITessageWithIdentity.Id"/>, read from <see cref="IPublisherTevent{TTevent}.Tevent"/>.</summary>
   void PublishTransactionally(IPublisherTevent<IExactlyOnceTevent> wrappedTevent);
}
