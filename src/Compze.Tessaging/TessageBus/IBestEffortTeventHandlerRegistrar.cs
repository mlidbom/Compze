using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal registrar for tevent subscriptions that do not demand exactly-once delivery — what an<br/>
/// endpoint-declaration's <c>RegisterBestEffortTeventHandlers</c> override receives. Both tiers' declarations offer that<br/>
/// override: best-effort delivery needs no durable vertical. A subscription whose subscribed type demands exactly-once<br/>
/// delivery explodes here at registration, pointing at <c>RegisterExactlyOnceTeventHandlers</c>.<br/>
/// Implemented by <see cref="TessageBusHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface IBestEffortTeventHandlerRegistrar
{
   ///<summary>Registers a handler for tevents compatible with <typeparamref name="TTevent"/>. The handler receives the<br/>
   /// <see cref="IUnitOfWorkResolver"/> of the unit of work delivering the tevent.</summary>
   IBestEffortTeventHandlerRegistrar ForTevent<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler) where TTevent : ITevent;

   ///<summary>The synchronous form of <see cref="ForTevent{TTevent}(Func{TTevent,IUnitOfWorkResolver,Task})"/> — first-class<br/>
   /// here, because no subscription registered here is exactly-once.</summary>
   IBestEffortTeventHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IUnitOfWorkResolver> handler) where TTevent : ITevent;
}
