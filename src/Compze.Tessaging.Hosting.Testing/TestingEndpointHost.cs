using Compze.Abstractions.Hosting.Public;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Transport;
using Compze.Threading;
using Compze.Underscore;

namespace Compze.Tessaging.Hosting.Testing;

///<summary>
/// The endpoint host tests use: an <see cref="EndpointHost"/> plus the per-tier test wiring that hands each registered
/// endpoint its test concerns at construction — the host's one tessages-in-flight tracker, the current test's transport
/// protocol and serializers, the pooled test database (exactly-once tier), and participation in the host's endpoint
/// registry: a real interprocess registry, the same announce/discover pipeline a production same-machine suite runs, private
/// to this host and deleted with it.
///
/// All endpoints are built from clones of one root container, so they share the test database pool and serializers. On
/// dispose the host waits until no tessages are in flight and rethrows any background exceptions no assertion observed, so
/// tests cannot silently drop in-flight work or background failures.
///</summary>
public class TestingEndpointHost : EndpointHost
{
   static readonly WaitTimeout EndpointsAtRestTimeout = WaitTimeout.Seconds(10);

   readonly TessagesInFlightTracker _tessagesInFlightTracker = new();
   readonly IDependencyInjectionContainer _rootContainer;
   readonly bool _ownsRootContainer;
   readonly DirectoryInfo _endpointRegistryDirectory;
   readonly InterprocessEndpointRegistry _endpointRegistry;

   ///<summary>The registry every endpoint in the host participates in: each announces its address here and discovers the<br/>
   /// others through it — backed by a real interprocess registry the host owns (created per host, deleted when the host is<br/>
   /// disposed), so a separate process can participate in the same suite by opening it.</summary>
   public IEndpointRegistryAndAnnouncer EndpointRegistry => _endpointRegistry;

   TestingEndpointHost(IDependencyInjectionContainer rootContainer, bool ownsRootContainer) : base(rootContainer.CreateCloneContainerBuilder)
   {
      _rootContainer = rootContainer;
      _ownsRootContainer = ownsRootContainer;

      _endpointRegistryDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), "Compze", "Tests", "TestingHostEndpointRegistries", Guid.NewGuid().ToString("N")))._mutate(it => it.Create());
      _endpointRegistry = InterprocessEndpointRegistry.OpenOrCreateSessionLocal("EndpointRegistry", _endpointRegistryDirectory);
   }

   ///<summary>Creates a testing host with its own root container, set up with the current test's DI container technology, serializers and database pool.</summary>
   public static TestingEndpointHost Create()
   {
      var rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                                 ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                                 .Build();
      return new TestingEndpointHost(rootContainer, ownsRootContainer: true);
   }

   ///<summary>Creates a testing host on a root container the test owns and will dispose itself.</summary>
   public static TestingEndpointHost Create(IDependencyInjectionContainer rootContainer) =>
      new(rootContainer, ownsRootContainer: false);

   ///<summary>Registers an <see cref="ExactlyOnceEndpoint"/> composed with the current test's concerns — the host's tracker,<br/>
   /// transport, serializers, the pooled test database (its connection string keyed by the endpoint's id, so an endpoint<br/>
   /// keeps its database across host rebuilds and specs can script restarts), and participation in<br/>
   /// <see cref="EndpointRegistry"/> — plus whatever <paramref name="declare"/> declares.</summary>
   public ExactlyOnceEndpoint RegisterExactlyOnceEndpoint(string name, EndpointId id, Action<ExactlyOnceEndpointBuilder> declare) =>
      RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, name, id, endpoint =>
      {
         DeclareTheCurrentTestsConcerns(endpoint);
         endpoint.Database(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: id.ToString()));
         declare(endpoint);
      }));

   ///<summary>Registers a <see cref="BestEffortEndpoint"/> composed with the current test's concerns — the host's tracker,<br/>
   /// transport, serializers, and participation in <see cref="EndpointRegistry"/> — plus whatever <paramref name="declare"/> declares.</summary>
   public BestEffortEndpoint RegisterBestEffortEndpoint(string name, EndpointId id, Action<BestEffortEndpointBuilder> declare) =>
      RegisterEndpoint(container => BestEffortEndpoint.Compose(container, name, id, endpoint =>
      {
         DeclareTheCurrentTestsConcerns(endpoint);
         declare(endpoint);
      }));

   void DeclareTheCurrentTestsConcerns(EndpointBuilder endpoint)
   {
      endpoint.TrackTessagesInFlightWith(_tessagesInFlightTracker);
      endpoint.TransportProtocol(registrar => registrar.CurrentTestsEndpointTransport());
      //The serializers arrive with the cloned root container; the topology with the host's registry.
      endpoint.ParticipateIn(_endpointRegistry);
   }

   bool _disposed;

#pragma warning disable CA1031 // We want to catch all exceptions and throw an aggregate if there are multiple
   protected override async ValueTask DisposeAsync(bool disposing) => await DisposeAsync(disposing, waitForEndpointsToBeAtRest: true).caf();

   async ValueTask DisposeAsync(bool disposing, bool waitForEndpointsToBeAtRest)
   {
      if(_disposed) return;
      _disposed = true;

      List<Exception> unobservedExceptions = [];
      if(waitForEndpointsToBeAtRest)
      {
         try
         {
            _tessagesInFlightTracker.AwaitNoTessagesInFlight(EndpointsAtRestTimeout);
         }
         catch(Exception e)
         {
            unobservedExceptions.Add(e);
         }
      }

      unobservedExceptions.AddRange(_tessagesInFlightTracker.GetExceptions());

      try
      {
         await base.DisposeAsync(disposing).caf();
      }
      catch(AggregateException aggregateException)
      {
         unobservedExceptions.AddRange(aggregateException.Flatten().InnerExceptions);
      }
      catch(Exception e)
      {
         unobservedExceptions.Add(e);
      }

      if(_ownsRootContainer)
      {
         try
         {
            await _rootContainer.DisposeAsync().caf();
         }
         catch(Exception e)
         {
            unobservedExceptions.Add(e);
         }
      }

      try
      {
         //After the endpoints are disposed: their retracting phase writes to the registry.
         _endpointRegistry.Dispose();
         _endpointRegistryDirectory.Delete(recursive: true);
      }
      catch(Exception e)
      {
         unobservedExceptions.Add(e);
      }

      if(unobservedExceptions.Any())
      {
         throw new AggregateException("Unhandled exceptions in testing endpoint host", unobservedExceptions);
      }
   }
#pragma warning restore CA1031

   ///<summary>Disposes without first waiting for the endpoints to come to rest — for tests that deliberately leave work in flight, such as queued tessages that must never be delivered.</summary>
   public async Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest() => await DisposeAsync(true, waitForEndpointsToBeAtRest: false).caf();
}
