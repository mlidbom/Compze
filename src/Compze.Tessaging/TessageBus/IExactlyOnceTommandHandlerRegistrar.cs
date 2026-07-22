using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal declaration door for exactly-once tommand handlers — what an endpoint-declaration's<br/>
/// <c>RegisterExactlyOnceTommandHandlers</c> override receives. It shows exactly one verb: registering the handler of an<br/>
/// <see cref="IExactlyOnceTommand"/>, the one tommand kind that is sent through the bus rather than navigated.<br/>
/// Implemented by <see cref="TessageBusHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface IExactlyOnceTommandHandlerRegistrar
{
   ///<summary>Registers the handler for <typeparamref name="TTommand"/>. The handler receives the <see cref="IUnitOfWorkResolver"/><br/>
   /// of the unit of work its execution IS — a tommand mutates state, so its effects commit or roll back as a whole.<br/>
   /// Async only, no synchronous form: exactly-once kinds are async end to end.</summary>
   IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : IExactlyOnceTommand;
}
