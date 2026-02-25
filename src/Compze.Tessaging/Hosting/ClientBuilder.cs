using System;
using System.Threading.Tasks;
using Compze.Core.Configuration.Internal;
using Compze.Core.Refactoring.Naming.Internal.Implementation;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Configuration;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Tessaging.Implementation.Transport.Client.Routing.Abstractions;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.Tessaging.Typermedia;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE;
using Compze.Utilities.SystemCE.ThreadingCE.TasksCE;

namespace Compze.Tessaging.Hosting;

class ClientBuilder : IEndpointBuilder, IAsyncDisposable, IDisposable
{
   bool _builtSuccessfully;

   public IDependencyInjectionContainer Container { get; }
   public EndpointConfiguration Configuration { get; }
   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   readonly IEndpointHost _host;
   readonly ITessagesInFlightTracker _globalStateTracker;

   public ClientBuilder(IEndpointHost host, ITessagesInFlightTracker globalStateTracker, IDependencyInjectionContainer container)
   {
      _host = host;
      _globalStateTracker = globalStateTracker;
      Container = container;

      // Configuration and RegisterHandlers exist to satisfy IEndpointBuilder but are not meaningful for clients
      Configuration = new EndpointConfiguration("Client", new EndpointId(Guid.Parse("D4C869D2-68EF-469C-A5D6-37FCF2EC152A")));
      RegisterHandlers = new TessageHandlerRegistrarWithDependencyInjectionSupport(
         new TessageHandlerRegistry(TypeMapper.Instance),
         new LazyCE<IServiceLocator>(() => Container.ServiceLocator));
   }

   public Client Build()
   {
      SetupContainer();
      var serviceLocator = Container.ServiceLocator;
      var client = new Client(serviceLocator);
      _builtSuccessfully = true;
      return client;
   }

   void SetupContainer()
   {
      var register = Container.Register();

      register.JSonAppConfigFileConfigurationParameterProvider()
              .TypeMapper();

      if(_host is IEndpointRegistry endpointRegistry)
      {
         Container.Register(Singleton.For<IEndpointRegistry>().Instance(endpointRegistry));
      } else
      {
         Container.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
      }

      register.Transport()
              .RemoteHypermediaNavigator();

      Container.Register(Singleton.For<ITessagesInFlightTracker>().CreatedBy(() => _globalStateTracker));
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
