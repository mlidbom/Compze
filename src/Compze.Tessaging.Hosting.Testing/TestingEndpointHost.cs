using Compze.Abstractions.Hosting.Public;
using Compze.Hosting;
using Compze.Hosting.SameMachine;
using Compze.Hosting.Testing;
using Compze.Hosting.Testing.Wiring;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.Hosting.Testing.Wiring;
using Compze.Tessaging.Implementation;
using Compze.Tessaging.Implementation.Peers;
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
   static readonly TimeSpan EndpointsMeetingTimeout = TimeSpan.FromSeconds(30);

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
      RegisterExactlyOnceEndpointInDomainDatabase(name, id, domainDatabaseName: id.ToString(), declare);

   ///<summary>Like <see cref="RegisterExactlyOnceEndpoint"/>, but the endpoint joins the named shared domain database instead<br/>
   /// of one of its own — the composition for several endpoints storing side by side in one domain database: each with its<br/>
   /// prefixed table-set, sharing the endpoint catalog and the type-id interner.</summary>
   public ExactlyOnceEndpoint RegisterExactlyOnceEndpointInDomainDatabase(string name, EndpointId id, string domainDatabaseName, Action<ExactlyOnceEndpointBuilder> declare) =>
      RegisterEndpoint(container => ExactlyOnceEndpoint.Compose(container, name, id, endpoint =>
      {
         DeclareTheCurrentTestsConcerns(endpoint);
         endpoint.DomainDatabase(registrar => registrar.CurrentTestsConfiguredSqlLayer(connectionStringName: domainDatabaseName));
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

   ///<summary>Awaits every endpoint of this host remembering every other endpoint of the host as a peer — mutual first<br/>
   /// contact. <see cref="EndpointHost.StartAsync"/> completes when every endpoint has started; whether the endpoints have<br/>
   /// *discovered each other* is topology convergence, completing at signal latency after the start. A specification whose<br/>
   /// very next act rides that discovery — an exactly-once tevent whose fan-out membership is the remembered subscribers<br/>
   /// (first contact is the boundary: a tevent published before a subscriber was ever met is not owed to it), an assertion<br/>
   /// over peer memory, an ambiguity pin needing every advertising handler visible — awaits this after starting, instead of<br/>
   /// racing the reconciliation. A production composition awaits what it needs through the production surfaces instead:<br/>
   /// readiness for the types it will send to, <c>RequirePeers</c> for best-effort tevents, waiting sends for the rest.</summary>
   public async Task AwaitEndpointsHaveMetEachOtherAsync()
   {
      var endpoints = Endpoints.OfType<Endpoint>().ToList();
      var deadline = DateTime.UtcNow + EndpointsMeetingTimeout;
      foreach(var endpoint in endpoints)
      {
         var peerRegistry = endpoint.ServiceLocator.Resolve<IPeerRegistry>();
         foreach(var other in endpoints.Where(other => !other.Id.Equals(endpoint.Id)))
         {
            while(!peerRegistry.Peers.Any(peer => peer.Id.Equals(other.Id)))
            {
               if(DateTime.UtcNow > deadline) throw new TimeoutException($"Endpoint '{endpoint.Id}' has not met endpoint '{other.Id}' within {EndpointsMeetingTimeout}.");
               await Task.Delay(TimeSpan.FromMilliseconds(10)).caf();
            }
         }
      }
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
