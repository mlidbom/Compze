using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Abstractions.Internal.Time;
using Compze.Common.Refactoring.Naming;
using Compze.Common.Refactoring.Naming.Wiring;
using Compze.Serialization;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Configuration;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Tessaging.Persistence.EventStore;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Teventive.EventStore.Abstractions;
using Compze.Tessaging.Typermedia;
using Compze.Tessaging.Typermedia.Abstractions;
using Compze.Wiring;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;

// ReSharper disable ImplicitlyCapturedClosure it is very much intentional :)

namespace Compze.Tessaging.Hosting;

class ServerEndpointBuilder : IEndpointBuilder
{
   bool _builtSuccessfully;

   public IDependencyInjectionContainer Container { get; }

   readonly IMessagesInFlightTracker _globalStateTracker;
   readonly MessageHandlerRegistry _registry;
   readonly IEndpointHost _host;
   public EndpointConfiguration Configuration { get; }

   public MessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   public IEndpoint Build()
   {
      SetupContainer();
      MessageTypesInternal.RegisterHandlers(RegisterHandlers);
      var serviceLocator = Container.ServiceLocator;
      var endpoint = new Endpoint(serviceLocator,
                                  serviceLocator.Resolve<IMessagesInFlightTracker>(),
                                  serviceLocator.Resolve<ITransport>(),
                                  serviceLocator.Resolve<IEndpointRegistry>(),
                                  Configuration);
      _builtSuccessfully = true;
      return endpoint;
   }

   public ServerEndpointBuilder(IEndpointHost host, IMessagesInFlightTracker globalStateTracker, IDependencyInjectionContainer container, EndpointConfiguration configuration)
   {
      _host = host;
      Container = container;
      _globalStateTracker = globalStateTracker;

      Configuration = configuration;

      _registry = new MessageHandlerRegistry(TypeMapper.Instance);
      RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(_registry, new OptimizedLazy<IServiceLocator>(() => Container.ServiceLocator));
   }

   //todo: find a better place for this. I just can't be bothered right now during a huge refactoring of other stuff.

   void SetupContainer()
   {
      //Universal stuff here
      Container.Register()
               .TimeSource()
               .JSonAppConfigFileConfigurationParameterProvider()
               .TypeMapper();

      //Only endpoint stuff after here
      //todo: Find cleaner way of doing this.
      if(_host is IEndpointRegistry endpointRegistry)
      {
         Container.Register(Singleton.For<IEndpointRegistry>().Instance(endpointRegistry));
      } else
      {
         Container.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
      }

      //Transport
      Container.Register().Register(RemotableMessageSerializer.RegisterWith,
                                    Transport.RegisterWith,
                                    RemoteHypermediaNavigator.RegisterWith,
                                    HttpClientFactoryCE.RegisterWith,
                                    HttpApiClient.RegisterWith);

      Container.Register(
         Singleton.For<IMessagesInFlightTracker>().CreatedBy(() => _globalStateTracker));

      //Only real endpoint stuff after here
      if(!Configuration.IsPureClientEndpoint)
      {
         Container.Register().Register(TaskRunner.RegisterWith,
                                       Outbox.RegisterWith);

         Container.Register(
            Singleton.For<IDependencyInjectionContainer>().CreatedBy(() => Container),
            Singleton.For<EndpointId>().CreatedBy(() => Configuration.Id),
            Singleton.For<EndpointConfiguration>().CreatedBy(() => Configuration),
            Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>().CreatedBy(() => _registry),
            Singleton.For<IEventStoreEventPublisher>().CreatedBy((IOutbox outbox, IMessageHandlerRegistry messageHandlerRegistry)
                                                                    => new ServiceBusEventStoreEventPublisher(outbox, messageHandlerRegistry)),
            Singleton.For<Inbox.IMessageStorage>().CreatedBy((IServiceBusPersistenceLayer.IInboxPersistenceLayer persistenceLayer) => new InboxMessageStorage(persistenceLayer)),
            Singleton.For<Inbox.HandlerExecutionEngine>().CreatedBy((IMessagesInFlightTracker globalStateTracker, IMessageHandlerRegistry handlerRegistry, IServiceLocator serviceLocator, Inbox.IMessageStorage storage, ITaskRunner taskRunner)
                                                                       => new Inbox.HandlerExecutionEngine(globalStateTracker, handlerRegistry, serviceLocator, storage, taskRunner)),
            Singleton.For<IInbox>().CreatedBy((IServiceLocator serviceLocator, Inbox.HandlerExecutionEngine handlerExecutionEngine, Inbox.IMessageStorage messageStorage, IDependencyInjectionContainer container, IInboxTransport transport)
                                                 => new Inbox(serviceLocator, handlerExecutionEngine, messageStorage, container, transport)),
            Singleton.For<CommandScheduler>().CreatedBy((IOutbox transport, IUtcTimeTimeSource timeSource, ITaskRunner taskRunner) => new CommandScheduler(transport, timeSource, taskRunner)),
            Scoped.For<IServiceBusSession>().CreatedBy((IOutbox outbox, CommandScheduler commandScheduler) => new ServiceBusSession(outbox, commandScheduler)),
            Scoped.For<ILocalHypermediaNavigator>().CreatedBy((IMessageHandlerRegistry messageHandlerRegistry) => new LocalHypermediaNavigator(messageHandlerRegistry))
         );
      }
   }

   bool _disposed;

   public void Dispose()
   {
      if(!_disposed)
      {
         _disposed = true;
         if(!_builtSuccessfully)
         {
            Container.Dispose();
         }
      }
   }
}
