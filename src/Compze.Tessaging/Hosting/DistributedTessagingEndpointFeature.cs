using Compze.Abstractions.Configuration.Internal;
using Compze.Abstractions.Hosting.Public;
using Compze.DependencyInjection;
using Compze.Internals.Transport;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.Outbox;
using Compze.Tessaging.Implementation.TessageHandling.Inbox;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.Tessaging.Implementation.Transport.Client.Implementation.Universal;
using Compze.Tessaging.Implementation.Transport.Client.Routing;
using Compze.Tessaging.SystemCE.ThreadingCE;

namespace Compze.Tessaging.Hosting;

///<summary>
/// Wires the distributed Tessaging pipeline — inbox, outbox, tommand scheduler, router, service bus
/// session — into an endpoint: everything tessage handling has
/// (<see cref="TessageHandlingEndpointFeature"/>, which it composes), plus the machinery through which the
/// endpoint converses with other endpoints and the distributed tevent publication mode
/// (<see cref="DistributedTeventStoreTeventPublisher"/>). Created idempotently through
/// <see cref="EndpointBuilderTessagingExtensions.AddDistributedTessaging"/> /
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: this is how distributed Tessaging plugs into a
/// hosting mechanism that knows nothing of it, and the feature instance is the handle through which the
/// endpoint's tessaging handlers are registered (<see cref="RegisterHandlers"/>).
///
/// Two registrations are guarded with <c>IsRegistered</c> so a hosting layer can pre-register its own before
/// the feature is added: the in-flight tracker (a testing host supplies a real one to await quiescence; the
/// default does nothing) and the <see cref="IEndpointRegistry"/> (a testing host lists its own endpoints; the
/// default reads application configuration). The runtime lifecycle lives in
/// <see cref="DistributedTessagingEndpointComponent"/>, and the endpoint's inbox address is exposed as the
/// <c>TessagingAddress</c> extension property (<see cref="EndpointTessagingExtensions"/>).
///</summary>
///<remarks>
/// Mutually exclusive with <see cref="InProcessTessagingEndpointFeature"/>: an endpoint declares exactly one
/// tevent publication mode, and declaring both fails loudly at setup time.
///</remarks>
public class DistributedTessagingEndpointFeature
{
   public TessageHandlerRegistrarWithDependencyInjectionSupport RegisterHandlers { get; }

   internal DistributedTessagingEndpointFeature(IEndpointBuilder builder)
   {
      var register = builder.Registrar;
      register.AssertNoTeventPublicationModeIsDeclared();

      RegisterHandlers = builder.AddTessageHandling().RegisterHandlers;

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
              .DistributedTeventStoreTeventPublisher()
              .ServiceBusSession();

      builder.OnContainerBuilt(resolver => TessageTypesInternal.RegisterInfrastructureQueryHandlers(
                                  new InfrastructureQueryRegistrarWithDependencyInjectionSupport(resolver.Resolve<InfrastructureQueryExecutor>())));

      builder.AddComponent(resolver => new DistributedTessagingEndpointComponent(resolver));
   }
}
