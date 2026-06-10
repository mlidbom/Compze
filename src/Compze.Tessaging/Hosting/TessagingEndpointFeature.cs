using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.Core.Tessaging.Teventive.Public.Taggregates.Tevents.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.Transport;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Dispatching;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting;

///<summary>Wires the Tessaging pipeline — inbox, outbox, tommand scheduler, router, service bus session — into an endpoint.</summary>
public class TessagingEndpointFeature
{
   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal TessagingEndpointFeature(IEndpointBuilder builder)
   {
      builder.TypeMapper.MapTypesFromAssemblyContaining<ITaggregateTevent>(); // Compze.Core — the Teventive type hierarchy

      var handlerRegistry = new TessageHandlerRegistry(builder.TypeMap);
      RegisterHandlers = new TessageHandlerRegistrarWithDependencyInjectionSupport(handlerRegistry);

      var register = builder.Registrar;
      register.Register(Singleton.For<ITessageHandlerRegistry, ITessageHandlerRegistrar>().Instance(handlerRegistry));

      if(!register.IsRegistered<ITessagesInFlightTracker>())
      {
         register.Register(Singleton.For<ITessagesInFlightTracker>().CreatedBy(() => new NullOpTessagesInFlightTracker()));
      }

      if(!register.IsRegistered<IEndpointRegistry>())
      {
         register.Register(Singleton.For<IEndpointRegistry>().CreatedBy((IConfigurationParameterProvider configurationParameterProvider) => new AppConfigEndpointRegistry(configurationParameterProvider)));
      }

      register.BackgroundExceptionReporter()
              .TaskRunner()
              .TessagingTransport()
              .Outbox()
              .Inbox()
              .TommandScheduler()
              .ServiceBusTeventStoreTeventPublisher()
              .ServiceBusSession();

      builder.OnContainerBuilt(resolver => TessageTypesInternal.RegisterInfrastructureQueryHandlers(
                                  new InfrastructureQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<InfrastructureQueryExecutor>())));

      builder.AddComponent(resolver => new TessagingEndpointComponent(resolver));
   }
}
