using Compze.Abstractions.Internal.Persistence.DocumentDb;
using Compze.DependencyInjection;
using Compze.EventStore.PersistenceLayer.Abstractions;
using Compze.Persistence.InMemory.DocumentDB;
using Compze.Persistence.InMemory.EventStore;
using Compze.Persistence.InMemory.ServiceBus;
using Compze.Tessaging.Hosting;
using Compze.Tessaging.Hosting.Implementation;

namespace Compze.Persistence.InMemory.DependencyInjection;

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