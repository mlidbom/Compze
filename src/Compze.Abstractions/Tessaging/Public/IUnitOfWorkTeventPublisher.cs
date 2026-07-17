namespace Compze.Abstractions.Tessaging.Public;

///<summary>Publishes tevents within the caller's unit of work: the publisher is scoped, participation delivers through the<br/>
/// caller's scope, and the caller's ambient transaction decides when remote delivery happens. Anything running inside a unit of<br/>
/// work can publish — a taggregate's tevent store forwarding its committed tevents is just this interface's most common client —<br/>
/// while code outside any unit of work publishes through <see cref="IIndependentTeventPublisher"/>, the independent counterpart<br/>
/// that gives each publish its own. The publisher routes each tevent by the delivery contract its type declares<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>):<br/>
/// every tevent is delivered to this process's subscribed handlers within the caller's transaction — the participation rung,<br/>
/// the strongest delivery there is — and queued for this process's observers when that transaction commits (observation,<br/>
/// outside every transaction);<br/>
/// an <see cref="IExactlyOnceTevent"/> additionally travels the durable delivery leg to its remote subscribers — through the<br/>
/// endpoint's outbox, on commit — and a remotable tevent whose type declares no exactly-once guarantee the best-effort leg —<br/>
/// best-effort, on commit — when the endpoint's composition wires them. An endpoint that wires no remote delivery is a<br/>
/// deliberately in-process composition: every subscriber is local and already served by participation.</summary>
///<remarks>The ambient transaction is required and honored: publishing with none present throws — there is no unit of work to<br/>
/// publish within, and <see cref="IIndependentTeventPublisher"/> is the door for such callers. Remote delivery happens only on<br/>
/// commit, so a rolled-back transaction never leaks a tevent — and participation's handlers run inside that same<br/>
/// transaction, so their effects roll back with it. A tevent published without a publisher-identifying wrapper<br/>
/// (<see cref="IPublisherTevent{TTevent}"/>) is wrapped before routing.</remarks>
///<remarks>Synchrony follows the type — the sync/async pair mirrors it: <see cref="PublishAsync"/> serves every tevent kind,<br/>
/// while <see cref="Publish"/> serves the kinds whose contract keeps sync first-class — strictly-local and best-effort — and<br/>
/// refuses an <see cref="IExactlyOnceTevent"/>, whose publish writes durable rows inside the caller's transaction: database<br/>
/// I/O, async end to end by its type's contract.</remarks>
public interface IUnitOfWorkTeventPublisher
{
   ///<summary>Publishes <paramref name="tevent"/> per the delivery contract its type declares — see the type's remarks for the<br/>
   /// sync/async split: an <see cref="IExactlyOnceTevent"/> is refused here, loudly, pointing at <see cref="PublishAsync"/>.</summary>
   void Publish(ITevent tevent);

   ///<summary>Publishes <paramref name="tevent"/> per the delivery contract its type declares, awaiting participation and the<br/>
   /// durable outbox write an <see cref="IExactlyOnceTevent"/>'s contract demands. The one form serving every tevent kind.</summary>
   Task PublishAsync(ITevent tevent);
}
