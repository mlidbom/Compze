using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal registrar for tevent subscriptions that demand exactly-once delivery — what an<br/>
/// endpoint-declaration's <c>RegisterExactlyOnceTeventHandlers</c> override receives. Only the exactly-once tier's<br/>
/// declaration offers that override: honoring the demand takes the durable vertical (inbox dedup, transactional retry) that<br/>
/// only that tier wires. Implemented by <see cref="TessageBusHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
///<remarks>Whether a subscription demands exactly-once delivery is declared by the subscribed type itself — directly<br/>
/// (<see cref="IExactlyOnceTevent"/>) or through its publisher-conscious wrapper (<see cref="IPublisherTevent{TTevent}"/> of an<br/>
/// exactly-once tevent) — which a generic constraint cannot express for the wrapper shape, so the fit is asserted at<br/>
/// registration instead: registering a subscription that does not demand exactly-once delivery here explodes,<br/>
/// pointing at <c>RegisterBestEffortTeventHandlers</c>.</remarks>
public interface IExactlyOnceTeventHandlerRegistrar
{
   ///<summary>Registers a handler for tevents compatible with <typeparamref name="TTevent"/> — a subscription whose subscribed<br/>
   /// type demands exactly-once delivery, asserted at declaration. The handler receives the <see cref="IUnitOfWorkResolver"/><br/>
   /// of the unit of work delivering the tevent. Async only, no synchronous form: exactly-once kinds are async end to end.</summary>
   IExactlyOnceTeventHandlerRegistrar ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler) where TTevent : ITevent;
}
