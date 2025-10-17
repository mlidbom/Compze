using Compze.Common.Configuration;
using Compze.Common.Refactoring.Naming;
using Compze.Common.Refactoring.Naming.Wiring;
using Compze.Serialization;
using Compze.Tessaging.Hosting.Abstractions;
using Compze.Tessaging.Hosting.Implementation;
using Compze.Tessaging.Hosting.Implementation.Abstractions;
using Compze.Tessaging.Hosting.Implementation.Http;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Typermedia;
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
      RegisterHandlers = new MessageHandlerRegistrarWithDependencyInjectionSupport(_registry, new LazyCE<IServiceLocator>(() => Container.ServiceLocator));
   }

   void SetupContainer()
   {
      var register = Container.Register();
      //Universal stuff here
      register.TimeSource()
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
      register.RemotableMessageSerializer()
              .Transport()
              .RemoteHypermediaNavigator()
              .HttpClientFactoryCE()
              .HttpApiClient();

      Container.Register(Singleton.For<IMessagesInFlightTracker>().CreatedBy(() => _globalStateTracker));

      //Only real endpoint stuff after here
      if(!Configuration.IsPureClientEndpoint)
      {
         register.BackgroundExceptionReporter()
                 .TaskRunner()
                 .Outbox()
                 .Inbox()
                 .CommandScheduler()
                 .ServiceBusEventStoreEventPublisher()
                 .ServiceBusSession()
                 .InProcessHypermediaNavigator();

         Container.Register(
            Singleton.For<IDependencyInjectionContainer>().Instance(Container),
            Singleton.For<EndpointConfiguration>().Instance(Configuration),
            Singleton.For<IMessageHandlerRegistry, IMessageHandlerRegistrar>().Instance(_registry)
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
