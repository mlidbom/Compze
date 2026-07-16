namespace Compze.Abstractions.Tessaging.Public;

///<summary>Publishes tevents within the caller's unit of work: the publisher is scoped, participation delivers through the<br/>
/// caller's scope, and the caller's ambient transaction decides when remote delivery happens. Anything running inside a unit of<br/>
/// work can publish — a taggregate's tevent store forwarding its committed tevents is just this interface's most common client —<br/>
/// while code outside any unit of work publishes through <see cref="IIndependentTeventPublisher"/>, the independent counterpart<br/>
/// that gives each publish its own. The publisher routes each tevent by the delivery contract its type declares<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>):<br/>
/// every tevent is delivered synchronously to this process's subscribed handlers, on the publishing thread, within the caller's<br/>
/// transaction — the participation rung, the strongest delivery there is — and immediately to this process's transaction-ignoring<br/>
/// handlers (observation, outside that transaction);<br/>
/// an <see cref="IExactlyOnceTevent"/> additionally travels the durable delivery leg to its remote subscribers — through the<br/>
/// endpoint's outbox, on commit — and a remotable tevent whose type declares no exactly-once guarantee the transient leg —<br/>
/// best-effort, on commit — when the endpoint's composition wires them. An endpoint that wires no remote delivery is a<br/>
/// deliberately in-process composition: every subscriber is local and already served by participation.</summary>
///<remarks>The ambient transaction is honored: remote delivery happens only on commit, so a rolled-back transaction never leaks<br/>
/// a tevent — and participation's synchronous handlers run inside that same transaction, so their effects roll back with it.<br/>
/// Only observation runs outside it, deliberately (<see cref="IUnitOfWorkIgnoringTeventPublisher"/> is the publish-side<br/>
/// counterpart). A tevent published without a publisher-identifying wrapper (<see cref="IPublisherTevent{TTevent}"/>) is wrapped<br/>
/// before routing.</remarks>
public interface IUnitOfWorkTeventPublisher
{
   ///<summary>Publishes <paramref name="tevent"/> per the delivery contract its type declares: synchronously to this process's<br/>
   /// subscribed handlers within the caller's transaction (plus its observers, outside it), and — on an endpoint whose composition<br/>
   /// wires remote delivery — to its remote subscribers on commit, durably for an <see cref="IExactlyOnceTevent"/> and best-effort<br/>
   /// for any other <see cref="IRemotableTevent"/>.</summary>
   void Publish(ITevent tevent);
}
