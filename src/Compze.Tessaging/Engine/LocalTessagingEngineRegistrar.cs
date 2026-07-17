using Compze.Abstractions.Tessaging.Public;
using Compze.Contracts;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Typermedia;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Engine;

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
   ///<remarks>When the composition declares no type mappings (<see cref="LocalTessagingEngineBuilder.MapTypes"/>) and the<br/>
   /// container registers no <see cref="ITypeMap"/> of its own, a default mapper with only the framework's mappings is<br/>
   /// supplied — local dispatch routes by <see cref="Type"/> and needs no mappings; persistent stores and endpoints do.</remarks>
   public static IComponentRegistrar LocalTessagingEngine(this IComponentRegistrar @this, Action<LocalTessagingEngineBuilder> compose)
   {
      State.Assert(!@this.IsRegistered<TessageHandlerRoster>(),
                   () => "This container already composes its LocalTessagingEngine — exactly one engine per container. Declare everything in the one composition block instead of composing a second engine.");

      var engineBuilder = new LocalTessagingEngineBuilder();
      compose(engineBuilder);

      RegisterTypeMapping(@this, engineBuilder);
      @this.RegisterLocalTessagingEngineCore(engineBuilder.HandlerRegistrations);
      return @this.UnitOfWorkTeventPublisher()
                  .IndependentTeventPublisher()
                  .LocalTypermediaNavigatorSession()
                  .IndependentLocalTypermediaNavigator();
   }

   static void RegisterTypeMapping(IComponentRegistrar registrar, LocalTessagingEngineBuilder engineBuilder)
   {
      if(engineBuilder.TypeMappingDeclarations.Count == 0 && registrar.IsRegistered<ITypeMap>()) return;

      var typeMapper = new TypeMapper();
      typeMapper.MapTypesFromAssemblyContaining<IExactlyOnceTevent>(); // Compze.Abstractions
      typeMapper.MapTypesFromAssemblyContaining<ITaggregateTevent>();  // Compze.Teventive — the Teventive type hierarchy
      engineBuilder.TypeMappingDeclarations.ForEach(declareMappings => declareMappings(typeMapper));
      registrar.Register(Singleton.For<ITypeMapper>().Instance(typeMapper),
                         Singleton.For<ITypeMap>().Instance(typeMapper));
   }
}
