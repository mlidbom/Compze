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
   ///<remarks>The handler also receives the token of the execution it serves: see<br/>
   /// <see cref="TessageBusHandlerRegistrar.ForTevent{TTevent}(Func{TTevent,IUnitOfWorkResolver,CancellationToken,Task},HandlingOrder)"/>.</remarks>
   ///<param name="handler">The handler: the tevent, the unit of work, and the token of the execution it serves.</param>
   ///<param name="order">Whether the tevents keep their place in the order the endpoint's inbox handles tessages in, where the<br/>
   /// inbox handles them: those published by another endpoint. A tevent gives up its place only when every handler it runs<br/>
   /// gives it up. See <see cref="HandlingOrder"/>.</param>
   IExactlyOnceTeventHandlerRegistrar ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted) where TTevent : ITevent;

   ///<summary>The form of <see cref="ForTevent{TTevent}(Func{TTevent,IUnitOfWorkResolver,CancellationToken,Task},HandlingOrder)"/><br/>
   /// for a handler that waits on nothing it could give up.</summary>
   IExactlyOnceTeventHandlerRegistrar ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted) where TTevent : ITevent
      => ForTevent<TTevent>((tevent, unitOfWork, _) => handler(tevent, unitOfWork), order);
}
