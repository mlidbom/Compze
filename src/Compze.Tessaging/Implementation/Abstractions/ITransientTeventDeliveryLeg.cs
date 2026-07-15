using Compze.Abstractions.Tessaging.Public;

namespace Compze.Tessaging.Implementation.Abstractions;

///<summary>The transient remote delivery leg for tevents: best-effort, in-order delivery to subscribers on other endpoints — no<br/>
/// store, no dedup, no retry (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). Wiring supplies the delivery legs —<br/>
/// this one is wired by the transport-speaking Tessaging core every transport-speaking composition shares (transient Tessaging is<br/>
/// that core alone; exactly-once Tessaging composes it and adds the durable sibling <see cref="IExactlyOnceTeventDeliveryLeg"/>) —<br/>
/// and the <see cref="ITeventPublisher"/> routes every published <see cref="IRemotableTevent"/> whose type declares no exactly-once<br/>
/// guarantee through it. An endpoint that wires no remote delivery is a deliberately in-process composition: its tevents are<br/>
/// delivered by participation alone.</summary>
interface ITransientTeventDeliveryLeg
{
   ///<summary>Publishes the wrapped tevent best-effort to every remote subscriber — the whole wrapper travels the wire, so publisher<br/>
   /// identity crosses endpoints with zero information loss. Honors the ambient transaction: with one present the tevent is handed to<br/>
   /// the subscribers' connections on commit — sent-on-commit without durability, so a rolled-back transaction never leaks a tevent —<br/>
   /// and with none present it is handed over immediately (the transient tier, unlike exactly-once, demands no transaction).</summary>
   void PublishBestEffort(IPublisherTevent<IRemotableTevent> wrappedTevent);
}
