using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Engine.HandlerRegistration;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Typermedia;

namespace Compze.Tessaging.Engine.Wiring;

///<summary>Composes a LocalTessagingEngine into a plain container — the tessage-conversing heart of one container, and for many<br/>
/// applications all of Tessaging they ever compose: tevents restructuring the internal flow of one process, local navigation<br/>
/// structuring its request handling, all of it on the caller's execution. No endpoint, no host, because there is nothing to<br/>
/// host: the engine has no identity, no address, and no lifecycle phases.</summary>
public static class LocalTessagingEngineRegistrar
{
   ///<summary>Composes the container's one LocalTessagingEngine from its declaration block: the<br/>
   /// <see cref="TessageHandlerRoster"/> built from the declared handlers, the one <see cref="TessageHandlerExecutor"/>, and<br/>
   /// the doors application code injects — <see cref="IUnitOfWorkTeventPublisher"/>/<see cref="IIndependentTeventPublisher"/><br/>
   /// for publishing tevents, <see cref="ILocalTypermediaNavigatorSession"/>/<see cref="IIndependentLocalTypermediaNavigator"/><br/>
   /// for strictly-local navigation. Exactly one engine per container: composing a second explodes.</summary>
   ///<remarks>The engine requires the type identity it needs where its core registers — local dispatch routes by<br/>
   /// <see cref="Type"/> and needs no mappings, but the persistent stores and endpoints that ride the engine do. A container<br/>
   /// whose domain types are persisted or transmitted declares them the same way, next to the components that need them.</remarks>
   public static IComponentRegistrar LocalTessagingEngine(this IComponentRegistrar @this, Action<LocalTessagingEngineBuilder> build)
   {
      State.Assert(!@this.IsRegistered<TessageHandlerRoster>(),
                   () => "This container already composes its LocalTessagingEngine — exactly one engine per container. Declare everything in the one composition block instead of composing a second engine.");

      var engineBuilder = new LocalTessagingEngineBuilder();
      build(engineBuilder);

      //A plain container has no testing host to await its at-rest, so the engine's observation bookkeeping reports to the null
      //device. (An endpoint composition declares the tracker its testing host hands it instead.)
      @this.Register(Singleton.For<ITessagesInFlightTracker>().Instance(new NullOpTessagesInFlightTracker()));
      @this.RegisterLocalTessagingEngineCore(engineBuilder.HandlerRegistrations);
      return @this.UnitOfWorkTeventPublisher()
                  .IndependentTeventPublisher()
                  .LocalTypermediaNavigatorSession()
                  .IndependentLocalTypermediaNavigator();
   }

}
