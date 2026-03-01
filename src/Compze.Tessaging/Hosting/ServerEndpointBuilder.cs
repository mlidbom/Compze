using System;
using System.Threading.Tasks;
using Compze.Core.Configuration.Internal;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Configuration;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Internal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Typermedia;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Threading.TasksCE;

// ReSharper disable ImplicitlyCapturedClosure it is very much intentional :)

namespace Compze.Tessaging.Hosting;

public class ServerEndpointBuilder : IEndpointBuilder, IAsyncDisposable, IDisposable
{
   bool _builtSuccessfully;

   public IDependencyInjectionContainer Container { get; }

   readonly ITessagesInFlightTracker _globalStateTracker;
   readonly TessageHandlerRegistry _registry;
   readonly IEndpointHost _host;
   public EndpointConfiguration Configuration { get; }

   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   public IEndpoint Build()
   {
      SetupContainer();
      TessageTypesInternal.RegisterHandlers(RegisterHandlers);
      var serviceLocator = Container.ServiceLocator;
      var endpoint = new Endpoint(serviceLocator,
                                  serviceLocator.Resolve<ITessagesInFlightTracker>(),
                                  serviceLocator.Resolve<ITessagingRouter>(),
                                  serviceLocator.Resolve<IEndpointRegistry>(),
                                  Configuration);
      _builtSuccessfully = true;
      return endpoint;
   }

   public ServerEndpointBuilder(IEndpointHost host, ITessagesInFlightTracker globalStateTracker, IDependencyInjectionContainer container, EndpointConfiguration configuration)
   {
      _host = host;
      Container = container;
      _globalStateTracker = globalStateTracker;

      Configuration = configuration;

      _registry = new TessageHandlerRegistry(TypeMapper.Instance);
      RegisterHandlers = new TessageHandlerRegistrarWithDependencyInjectionSupport(_registry, new LazyCE<IServiceLocator>(() => Container.ServiceLocator));
   }

   void SetupContainer()
   {
      var register = Container.Register();
      //Universal stuff here
      register.JSonAppConfigFileConfigurationParameterProvider()
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
      register.TessagingTransport();

      Container.Register(Singleton.For<ITessagesInFlightTracker>().CreatedBy(() => _globalStateTracker));

      register.BackgroundExceptionReporter()
              .TaskRunner()
              .Outbox()
              .Inbox()
              .TommandScheduler()
              .ServiceBusTeventStoreTeventPublisher()
              .ServiceBusSession()
              .InProcessHypermediaNavigator();

      Container.Register(
         Singleton.For<EndpointId>().Instance(Configuration.Id),
         Singleton.For<IDependencyInjectionContainer>().Instance(Container),
         Singleton.For<EndpointConfiguration>().Instance(Configuration),
         Singleton.For<ITessageHandlerRegistry, ITessageHandlerRegistrar>().Instance(_registry)
      );
   }

   bool _disposed;

   public async ValueTask DisposeAsync()
   {
      if(!_disposed)
      {
         _disposed = true;
         if(!_builtSuccessfully)
         {
            await Container.DisposeAsync().caf();
         }
      }
   }

   public void Dispose() => DisposeAsync().WaitUnwrappingException();
}