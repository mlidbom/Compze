using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.TypeIdentifiers;
using Compze.Typermedia.HandlerRegistration;

namespace Compze.Typermedia;

///<summary>
/// Composes in-process Typermedia into a plain container — no endpoint, no host, because there is nothing to
/// host: strictly local tueries and tommands execute synchronously on the calling thread, so there are no
/// transports to start and no background work to drive.
///</summary>
public static class InProcessTypermediaRegistrar
{
   ///<summary>
   /// Wires in-process Typermedia into the container being built: the handler registry and the
   /// <see cref="IInProcessTypermediaNavigator"/> through which strictly local tueries and tommands execute
   /// synchronously, on the calling thread, within the caller's transaction.<br/>
   /// Register handlers after the container is built, through the resolved
   /// <see cref="ITypermediaHandlerRegistrar"/>.
   ///</summary>
   ///<remarks>
   /// In-process navigation routes by <see cref="Type"/> and needs no type-id mappings, so when no
   /// <see cref="ITypeMap"/> is registered an empty default is supplied. An application that does need
   /// type-id mappings registers its own type mapper before this call.
   ///</remarks>
   public static IComponentRegistrar InProcessTypermedia(this IComponentRegistrar @this)
   {
      if(!@this.IsRegistered<ITypeMap>()) RegisterEmptyDefaultTypeMapper();

      return @this.TypermediaHandlerRegistry()
                  .InProcessTypermediaNavigator();

      void RegisterEmptyDefaultTypeMapper()
      {
         var typeMapper = new TypeMapper();
         @this.Register(Singleton.For<ITypeMapper>().Instance(typeMapper),
                        Singleton.For<ITypeMap>().Instance(typeMapper));
      }
   }
}
