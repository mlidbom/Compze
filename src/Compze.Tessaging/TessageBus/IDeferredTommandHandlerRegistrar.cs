using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.TessageBus;

///<summary>The minimal registrar for deferred tommand handlers — what an exactly-once endpoint declaration's<br/>
/// <c>RegisterDeferredTommandHandlers</c> override receives. A deferred handler never runs in its sender's unit of work: the<br/>
/// endpoint's inbox runs it in one of its own, once the sender has committed. Implemented by<br/>
/// <see cref="DeferredTessageBusHandlerRegistrar"/>, whose docs carry the full handler semantics.</summary>
public interface IDeferredTommandHandlerRegistrar
{
   //review-naming: IDeferredTommandHandlerRegistrar, after Magnus's choice of Deferred (2026-11-27).

   ///<summary>Registers the deferred handler for <typeparamref name="TTommand"/>: see<br/>
   /// <see cref="DeferredTessageBusHandlerRegistrar.ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task})"/>.</summary>
   ///<param name="handler">The handler: the tommand, the unit of work, and the token of the inbox's handling, cancelled when the<br/>
   /// endpoint's stop no longer waits for its inbox (<see cref="Endpoints.IEndpoint.StopAsync"/>).</param>
   IDeferredTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler) where TTommand : IExactlyOnceTommand;

   ///<summary>The form of <see cref="ForTommand{TTommand}(Func{TTommand,IUnitOfWorkResolver,CancellationToken,Task})"/> for a<br/>
   /// handler that waits on nothing it could give up.</summary>
   IDeferredTommandHandlerRegistrar ForTommand<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : IExactlyOnceTommand
      => ForTommand<TTommand>((tommand, unitOfWork, _) => handler(tommand, unitOfWork));
}
