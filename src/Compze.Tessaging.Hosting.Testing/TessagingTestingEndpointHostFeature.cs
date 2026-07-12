using Compze.Abstractions.Hosting.Public;
using Compze.Hosting.Testing;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation.Transport;
using Compze.Tessaging.Implementation.Transport.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Threading;
using Compze.Contracts;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>
/// Plugs distributed Tessaging into a <see cref="TestingEndpointHost"/>. Every endpoint the host registers gets
/// the distributed Tessaging pipeline (via <see cref="EndpointBuilderTessagingExtensions.AddDistributedTessaging"/>), the current test's
/// Tessaging transport and persistence, an <see cref="IEndpointRegistry"/> listing the host's tessaging inbox
/// addresses (so routers connect to every endpoint in the host), and a host-wide
/// <see cref="ITessagesInFlightTracker"/>. At dispose the host waits until no tessages are in flight and rethrows
/// any exceptions tessage handling produced in the background.
///</summary>
public class TessagingTestingEndpointHostFeature : ITestingEndpointHostFeature
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
             .Register(Singleton.For<IEndpointRegistry>().Instance(new TestingHostEndpointRegistry(() => _host._assert().NotNull().Endpoints)))
             .CurrentTestsTessagingTransport()
             .CurrentTestsConfiguredSqlLayer(connectionStringName: builder.Configuration.Id.ToString());

      builder.AddDistributedTessaging();
   }

   public void AwaitEndpointsAtRest() => _tessagesInFlightTracker.AwaitNoTessagesInFlight(EndpointsAtRestTimeout);

   public IReadOnlyList<Exception> GetBackgroundExceptions() => _tessagesInFlightTracker.GetExceptions();

   ///<summary>Knows the tessaging inbox addresses of every endpoint in the testing host, so that tessaging routers connect to all of them.</summary>
   class TestingHostEndpointRegistry(Func<IReadOnlyList<IEndpoint>> hostEndpoints) : IEndpointRegistry
   {
      readonly Func<IReadOnlyList<IEndpoint>> _hostEndpoints = hostEndpoints;

      public IEnumerable<EndpointAddress> ServerEndpointAddresses => _hostEndpoints().Where(it => it.TessagingAddress is not null)
                                                                                     .Select(it => it.TessagingAddress!)
                                                                                     .ToList();
   }
}
