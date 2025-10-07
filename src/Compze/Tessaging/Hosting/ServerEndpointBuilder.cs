using Compze.Abstractions.Internal.Refactoring.Naming;
using Compze.Abstractions.Internal.Time;
using Compze.Common.Refactoring.Naming;
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
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.SystemCE;

// ReSharper disable ImplicitlyCapturedClosure it is very much intentional :)

namespace Compze.Tessaging.Hosting;

class ServerEndpointBuilder : IEndpointBuilder
{
   readonly TypeMapper _typeMapper;
   bool _builtSuccessfully;

   public IDependencyInjectionContainer Container { get; }

   public ITypeMappingRegistrar TypeMapper => _typeMapper;
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

      _typeMapper = new TypeMapper();

      _registry = new MessageHandlerRegistry(_typeMapper);
      RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(_registry, new OptimizedLazy<IServiceLocator>(() => Container.ServiceLocator));
   }

   //todo: find a better place for this. I just can't be bothered right now during a huge refactoring of other stuff.

   void SetupContainer()
   {
      //todo: Find cleaner way of doing this.
      if(_host is IEndpointRegistry endpointRegistry)
      {
         Container.Register(Singleton.For<IEndpointRegistry>().Instance(endpointRegistry));
      } else
      {
         Container.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
      }

      // ReSharper disable once ConvertIfStatementToConditionalTernaryExpression
      if(Container.RunMode == RunMode.Production)
      {
         Container.Register(Singleton.For<IUtcTimeTimeSource>().CreatedBy(() => new DateTimeNowTimeSource()).DelegateToParentServiceLocatorWhenCloning());
      } else
      {
         Container.Register(Singleton.For<IUtcTimeTimeSource, TestingTimeSource>().CreatedBy(() => TestingTimeSource.FollowingSystemClock).DelegateToParentServiceLocatorWhenCloning());
      }

      Container.Register(Singleton.For<IConfigurationParameterProvider>().CreatedBy(() => new AppSettingsJsonConfigurationParameterProvider()));

      Container.Register(
         Singleton.For<ITypeMappingRegistrar, ITypeMapper, TypeMapper>().CreatedBy(() => _typeMapper).DelegateToParentServiceLocatorWhenCloning(),
         Singleton.For<IMessagesInFlightTracker>().CreatedBy(() => _globalStateTracker),
         Singleton.For<IRemotableMessageSerializer>().CreatedBy((ITypeMapper typeMapper) => new RemotableMessageSerializer(typeMapper)),
         Singleton.For<ITransport>().CreatedBy((IMessagesInFlightTracker messagesInFlightTracker, ITypeMapper typeMapper, IRemotableMessageSerializer serializer, IHttpApiClient httpApiClient)
                                                  => new Transport(messagesInFlightTracker, typeMapper, serializer, httpApiClient)),
         Scoped.For<IRemoteHypermediaNavigator>().CreatedBy((ITransport transport) => new RemoteHypermediaNavigator(transport)),
         Singleton.For<IHttpClientFactoryCE>().CreatedBy(() => new HttpClientFactoryCE()),
         Singleton.For<IHttpApiClient>().CreatedBy((IHttpClientFactoryCE factory, IRemotableMessageSerializer serializer) => new HttpApiClient(factory, serializer))
      );

      if(!Configuration.IsPureClientEndpoint)
      {
         Container.Register(
            Singleton.For<IDependencyInjectionContainer>().CreatedBy(() => Container),
            Singleton.For<EndpointId>().CreatedBy(() => Configuration.Id),
            Singleton.For<EndpointConfiguration>().CreatedBy(() => Configuration),
            Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar, MessageHandlerRegistry>().CreatedBy(() => _registry),
            Singleton.For<ITaskRunner>().CreatedBy(() => new TaskRunner()),
            Singleton.For<Outbox.IMessageStorage>()
                     .CreatedBy((IServiceBusPersistenceLayer.IOutboxPersistenceLayer persistenceLayer, ITypeMapper typeMapper, IRemotableMessageSerializer serializer)
                                   => new Outbox.MessageStorage(persistenceLayer, typeMapper, serializer)),
            Singleton.For<IOutbox>().CreatedBy((EndpointConfiguration configuration, ITransport transport, Outbox.IMessageStorage messageStorage)
                                                  => new Outbox(transport, messageStorage, configuration)),
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
