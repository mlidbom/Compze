using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

///<summary>The handler of a tommand type that declares no result, as the <see cref="TessageHandlerRoster"/> holds it: the handler,<br/>
/// whether it is deferred, and its <see cref="HandlingOrder"/>.</summary>
sealed class VoidTommandHandlerRegistration
{
   internal VoidTommandHandlerRegistration(Func<ITommand, IUnitOfWorkResolver, CancellationToken, Task> handle, bool isDeferred, HandlingOrder order)
   {
      Handle = handle;
      IsDeferred = isDeferred;
      Order = order;
   }

   internal Func<ITommand, IUnitOfWorkResolver, CancellationToken, Task> Handle { get; }

   ///<summary>Whether the handler never runs in its sender's unit of work, since the endpoint's inbox runs it in one of its own<br/>
   /// once the sender has committed: see <see cref="DeferredTessageBusHandlerRegistrar"/>.</summary>
   internal bool IsDeferred { get; }

   ///<summary>Whether the tommands keep their place in the order the endpoint's inbox handles tessages in.</summary>
   internal HandlingOrder Order { get; }
}
