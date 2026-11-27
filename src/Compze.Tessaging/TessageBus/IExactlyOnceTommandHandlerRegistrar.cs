using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal registrar for exactly-once tommand handlers — what an endpoint-declaration's<br/>
/// <c>RegisterExactlyOnceTommandHandlers</c> override receives. It shows exactly one verb: registering the handler of an<br/>
/// <see cref="IExactlyOnceTommand"/>, the one tommand kind that is sent through the bus rather than navigated.<br/>
/// Implemented by <see cref="TessageBusHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface IExactlyOnceTommandHandlerRegistrar
{
   ///<summary>Registers the handler for <typeparamref name="TTommand"/>. The handler receives the <see cref="IUnitOfWorkResolver"/><br/>
   /// of the unit of work its execution IS — a tommand mutates state, so its effects commit or roll back as a whole.<br/>
   /// Async only, no synchronous form: exactly-once kinds are async end to end.</summary>
   ///<remarks>The handler also receives the token of the execution it serves: see<br/>
   /// <see cref="TessageBusHandlerRegistrar.ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task})"/>.</remarks>
   ///<param name="handler">The handler: the tommand, the unit of work, and the token of the execution it serves.</param>
   IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler) where TTommand : IExactlyOnceTommand;

   ///<summary>The form of <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task})"/> for a<br/>
   /// handler that waits on nothing it could give up.</summary>
   IExactlyOnceTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : IExactlyOnceTommand
      => ForTommand<TTommand>((tommand, unitOfWork, _) => handler(tommand, unitOfWork));
}
