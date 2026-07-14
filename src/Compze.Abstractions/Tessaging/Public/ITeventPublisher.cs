namespace Compze.Abstractions.Tessaging.Public;

///<summary>The one way to publish a tevent. Anything can publish — a taggregate's tevent store forwarding its committed tevents<br/>
/// is just this interface's most common client — and the publisher routes each tevent by the delivery contract its type declares<br/>
/// (see <c>src/Compze.Tessaging/_docs/tevent-delivery-model.md</c>):<br/>
/// every tevent is delivered synchronously to this process's subscribed handlers, on the publishing thread, within the caller's<br/>
/// transaction — the participation rung, the strongest delivery there is;<br/>
/// an <see cref="IExactlyOnceTevent"/> additionally travels the durable delivery leg to its remote subscribers — through the<br/>
/// endpoint's outbox, on commit — when the endpoint's composition wires one. An endpoint that wires no remote delivery is a<br/>
/// deliberately in-process composition: every subscriber is local and already served by participation.</summary>
///<remarks>The ambient transaction is honored: remote delivery happens only on commit, so a rolled-back transaction never leaks<br/>
/// a tevent — and participation's synchronous handlers run inside that same transaction, so their effects roll back with it.<br/>
/// A tevent published without a publisher-identifying wrapper (<see cref="IPublisherTevent{TTevent}"/>) is wrapped<br/>
/// before routing.</remarks>
public interface ITeventPublisher
{
   ///<summary>Publishes <paramref name="tevent"/> per the delivery contract its type declares: synchronously to this process's<br/>
   /// subscribed handlers within the caller's transaction, and — for an <see cref="IExactlyOnceTevent"/> on an endpoint whose<br/>
   /// composition wires durable remote delivery — to its remote subscribers on commit.</summary>
   void Publish(ITevent tevent);
}
