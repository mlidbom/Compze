using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Refactoring.Naming.Internal.Implementation;
using Compze.Abstractions.Tessaging.Hosting.Public;
using Compze.Core.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.Public;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Typermedia;
using Compze.Typermedia.Client;
using Compze.Typermedia.Hosting;
using Compze.Tessaging.Configuration;
using Compze.Tessaging.Hosting;
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
using Compze.Internals.Transport;
using Compze.Tessaging.SystemCE.ThreadingCE;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Typermedia.HandlerRegistration;

// ReSharper disable ImplicitlyCapturedClosure it is very much intentional :)

namespace Compze.Hosting;

class ServerEndpointBuilder : IEndpointBuilder
{
   internal IContainerBuilder Builder { get; }
   public IComponentRegistrar Registrar => Builder.Registrar;

   readonly ITessagesInFlightTracker _globalStateTracker;
   readonly TessageHandlerRegistry _tessagingRegistry;
   readonly TypermediaHandlerRegistry _typermediaRegistry;
   readonly IEndpointHost _host;
   public EndpointConfiguration Configuration { get; }

   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterTessagingHandlers { get; }
   public TypermediaHandlerRegistrarWithDependencyInjectionSupport RegisterTypermediaHandlers { get; }

   public IEndpoint Build()
   {
      SetupContainer();
      var container = Builder.Build();
      var rootResolver = container.RootResolver;
      RegisterInfrastructureQueryHandlers(rootResolver);
      var endpoint = new Endpoint(container,
                                  rootResolver.Resolve<ITessagingRouter>(),
                                  rootResolver.Resolve<IEndpointRegistry>(),
                                  Configuration);
      return endpoint;
   }

   void RegisterInfrastructureQueryHandlers(IRootResolver rootResolver)
   {
      var executor = rootResolver.Resolve<InfrastructureQueryExecutor>();
      var registrar = new InfrastructureQueryRegistrarWithDependencyInjectionSupport(executor);
      TessageTypesInternal.RegisterInfrastructureQueryHandlers(registrar);
      TypermediaInfrastructureQueryRegistration.RegisterQueryHandlers(registrar);
   }

   public ServerEndpointBuilder(IEndpointHost host, ITessagesInFlightTracker globalStateTracker, IContainerBuilder builder, EndpointConfiguration configuration)
   {
      _host = host;
      Builder = builder;
      _globalStateTracker = globalStateTracker;

      Configuration = configuration;

      _tessagingRegistry = new TessageHandlerRegistry(TypeMapper.Instance);
      _typermediaRegistry = new TypermediaHandlerRegistry(TypeMapper.Instance);
      RegisterTessagingHandlers = new TessageHandlerRegistrarWithDependencyInjectionSupport(_tessagingRegistry);
      RegisterTypermediaHandlers = new TypermediaHandlerRegistrarWithDependencyInjectionSupport(_typermediaRegistry);
   }

   void SetupContainer()
   {
      var register = Registrar;
      //Universal stuff here
      register.JSonAppConfigFileConfigurationParameterProvider()
              .TypeMapper();

      //Only endpoint stuff after here
      //todo: Find cleaner way of doing this.
      if(_host is IEndpointRegistry endpointRegistry)
      {
         register.Register(Singleton.For<IEndpointRegistry>().Instance(endpointRegistry));
      } else
      {
         register.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
      }

      //Transport
      register.TessagingTransport();

      register.Register(Singleton.For<ITessagesInFlightTracker>().CreatedBy(() => _globalStateTracker));

      register.BackgroundExceptionReporter()
              .TaskRunner()
              .Outbox()
              .Inbox()
              .TommandScheduler()
              .ServiceBusTeventStoreTeventPublisher()
              .ServiceBusSession()
              .InProcessTypermediaNavigator();

      TypermediaHandlerExecutor.RegisterWith(register);
      InfrastructureQueryExecutor.RegisterWith(register);

      register.Register(
         Singleton.For<EndpointId>().Instance(Configuration.Id),
         Singleton.For<EndpointConfiguration>().Instance(Configuration),
         Singleton.For<ITessageHandlerRegistry, ITessageHandlerRegistrar>().Instance(_tessagingRegistry),
         Singleton.For<ITypermediaHandlerRegistry, ITypermediaHandlerRegistrar>().Instance(_typermediaRegistry)
      );
   }

}