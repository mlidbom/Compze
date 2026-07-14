using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Teventive.Taggregates.Tevents.Public;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging;

///<summary>
/// Composes in-process Tessaging into a plain container — no endpoint, no host, because there is nothing to
/// host: everything runs synchronously on the publishing thread, so there are no transports to start and no
/// background work to drive.
///</summary>
public static class InProcessTessagingRegistrar
{
   ///<summary>
   /// Wires in-process Tessaging into the container being built: the handler registry, the synchronous
   /// in-process tevent delivery every tevent travels, and the container's one
   /// <see cref="Compze.Abstractions.Tessaging.Public.ITeventPublisher"/>. The composition wires no remote
   /// delivery legs, so every published tevent — a taggregate's committed tevents included — is delivered
   /// synchronously to this process's handlers, within the publisher's transaction.<br/>
   /// Register handlers after the container is built, through the resolved
   /// <see cref="Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public.ITessageHandlerRegistrar"/>.
   ///</summary>
   ///<remarks>
   /// In-process dispatch routes by <see cref="Type"/> and needs no type-id mappings, so when no
   /// <see cref="ITypeMap"/> is registered a default one with only the framework's mappings is supplied.
   /// A domain that does need its own type-id mappings — for example because a persistent tevent store
   /// serializes its tevents — registers its own type mapper before this call.
   ///</remarks>
   public static IComponentRegistrar InProcessTessaging(this IComponentRegistrar @this)
   {
      if(!@this.IsRegistered<ITypeMap>()) RegisterDefaultTypeMapper();

      return @this.TessageHandlerRegistry()
                  .BackgroundExceptionReporter()
                  .InProcessTeventPublisher()
                  .TeventObservationDispatcher()
                  .TeventPublisher()
                  .TransactionIgnoringTeventPublisher();

      void RegisterDefaultTypeMapper()
      {
         var typeMapper = new TypeMapper();
         typeMapper.MapTypesFromAssemblyContaining<IExactlyOnceTevent>(); // Compze.Abstractions
         typeMapper.MapTypesFromAssemblyContaining<ITaggregateTevent>();  // Compze.Core — the Teventive type hierarchy
         @this.Register(Singleton.For<ITypeMapper>().Instance(typeMapper),
                        Singleton.For<ITypeMap>().Instance(typeMapper));
      }
   }
}
