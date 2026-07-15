using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.Threading;
using Compze.Contracts;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>
/// Plugs exactly-once Tessaging into a <see cref="TestingEndpointHost"/>. Every endpoint the host registers gets
/// the full exactly-once Tessaging pipeline (via <see cref="EndpointBuilderTessagingExtensions.AddExactlyOnceTessaging"/>), the current test's
/// Tessaging transport and persistence, participation in the host's endpoint registry
/// (<see cref="ITestingEndpointHost.EndpointRegistry"/> — every endpoint announces itself and discovers the
/// others through the same announce/discover pipeline a production same-machine suite runs, so routers connect
/// to every endpoint in the host), and a host-wide <see cref="ITessagesInFlightTracker"/>. At dispose the host
/// waits until no tessages are in flight and rethrows any exceptions tessage handling produced in the background.
///</summary>
public class ExactlyOnceTessagingTestingEndpointHostFeature : ITestingEndpointHostFeature
{
   static readonly WaitTimeout EndpointsAtRestTimeout = WaitTimeout.Seconds(10);

   readonly TessagesInFlightTracker _tessagesInFlightTracker = new();
   ITestingEndpointHost? _host;

   public void OnAddedToHost(ITestingEndpointHost host) => _host = host;

   public void SetupEndpoint(IEndpointBuilder builder)
   {
      //Endpoints need a consistent connection string or things go belly up when creating a new host with a new container.
      builder.Registrar
             .Register(Singleton.For<ITessagesInFlightTracker>().Instance(_tessagesInFlightTracker))
             .CurrentTestsEndpointTransport()
             .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());

      builder.AddExactlyOnceTessaging()
             .ParticipateIn(_host._assert().NotNull().EndpointRegistry);
   }

   public void AwaitEndpointsAtRest() => _tessagesInFlightTracker.AwaitNoTessagesInFlight(EndpointsAtRestTimeout);

   public IReadOnlyList<Exception> GetBackgroundExceptions() => _tessagesInFlightTracker.GetExceptions();
}
