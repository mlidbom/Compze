using Compze.Tessaging.Abstractions.TessageBus;
using Compze.Tessaging.Abstractions.TessageTypes;

namespace Compze.Tessaging.Internals.Abstractions;

///<summary>The best-effort remote delivery leg for tevents: in-order delivery to the remembered subscribers on other endpoints,<br/>
/// queued in memory while a subscribing peer is down and drained on its return — no durable store, no dedup, nothing ever<br/>
/// re-sent (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). Wiring supplies the delivery legs —<br/>
/// this one is wired by the transport-speaking Tessaging core every transport-speaking composition shares (distributed Tessaging is<br/>
/// that core alone; exactly-once Tessaging composes it and adds the durable sibling <see cref="IExactlyOnceTeventDeliveryLeg"/>) —<br/>
/// and the <see cref="IUnitOfWorkTeventPublisher"/> routes every published <see cref="IRemotableTevent"/> whose type declares no exactly-once<br/>
/// guarantee through it. An endpoint that wires no remote delivery is a deliberately in-process composition: its tevents are<br/>
/// delivered by participation alone.</summary>
interface IBestEffortTeventDeliveryLeg
{
   ///<summary>Publishes the wrapped tevent best-effort to every remembered remote subscriber — the whole wrapper travels the wire,<br/>
   /// so publisher identity crosses endpoints with zero information loss. Honors the caller's ambient transaction, which the<br/>
   /// publisher asserts: the tevent enters the subscribers' queues on commit — sent-on-commit without durability, so a rolled-back<br/>
   /// transaction never leaks a tevent.</summary>
   void PublishBestEffort(IPublisherTevent<IRemotableTevent> wrappedTevent);
}
