using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Persistence.InMemory.DocumentDB;
using Compze.Tessaging.Persistence.InMemory.EventStore;
using Compze.Tessaging.Persistence.InMemory.ServiceBus;
using Compze.Tessaging.Teventive.EventStore.PersistenceLayer.Abstractions;
using Compze.Utilities.DependencyInjection;

namespace Compze.Tessaging.Persistence.InMemory.DependencyInjection;

public static class InMemoryPersistenceLayerRegistrar
{
   public static void RegisterInMemoryPersistenceLayer(this IEndpointBuilder @this) => @this.Container.RegisterInMemoryPersistenceLayer(@this.Configuration.ConnectionStringName);

   public static void RegisterInMemoryPersistenceLayer(this IDependencyInjectionContainer container, string _)
   {
      //DocumentDB
      container.Register(
         Singleton.For<IDocumentDbPersistenceLayer>()
                  .CreatedBy(() => new InMemoryDocumentDbPersistenceLayer())
                  .DelegateToParentServiceLocatorWhenCloning());

      //Event store
      container.Register(Singleton.For<IEventStorePersistenceLayer>()
                                  .CreatedBy(() => new InMemoryEventStorePersistenceLayer())
                                  .DelegateToParentServiceLocatorWhenCloning());

      //Service bus
      container.Register(
         Singleton.For<IServiceBusPersistenceLayer.IOutboxPersistenceLayer>()
                  .CreatedBy(() => new InMemoryOutboxPersistenceLayer())
                  .DelegateToParentServiceLocatorWhenCloning(),
         Singleton.For<IServiceBusPersistenceLayer.IInboxPersistenceLayer>()
                  .CreatedBy(() => new InMemoryInboxPersistenceLayer())
                  .DelegateToParentServiceLocatorWhenCloning());
   }
}