using Compze.Abstractions.Hosting.Public;
using Compze.Hosting;
using Compze.Hosting.Testing.Wiring;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.LinqCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Internals.Testing;
using Compze.Underscore;

namespace Compze.Hosting.Testing;

///<summary>
/// The endpoint host tests use. Paradigm-blind: the <see cref="ITestingEndpointHostFeature"/>s it is created with
/// decide what every test endpoint is wired with — create it with the Tessaging feature, the Typermedia feature,
/// or both, and every endpoint registered with the host gets those pipelines plus the current test's pluggable
/// components, without each test repeating the wiring.
///
/// All endpoints are built from clones of one root container, so they share the test database pool and
/// serializers. On dispose the host waits for the features' background work to come to rest and rethrows any
/// background exceptions, so tests cannot silently drop in-flight work or background failures.
///</summary>
public class TestingEndpointHost : EndpointHost, ITestingEndpointHost
{
   readonly IReadOnlyList<ITestingEndpointHostFeature> _features;
   readonly IDependencyInjectionContainer _rootContainer;
   readonly bool _ownsRootContainer;

   TestingEndpointHost(IDependencyInjectionContainer rootContainer, bool ownsRootContainer, IReadOnlyList<ITestingEndpointHostFeature> features) : base(rootContainer.CreateCloneContainerBuilder)
   {
      _rootContainer = rootContainer;
      _ownsRootContainer = ownsRootContainer;
      _features = features;
      _features.ForEach(feature => feature.OnAddedToHost(this));
   }

   ///<summary>Creates a testing host with its own root container, set up with the current test's DI container technology, serializers and database pool.</summary>
   public static ITestingEndpointHost Create(params IReadOnlyList<ITestingEndpointHostFeature> features)
   {
      var rootContainer = TestEnv.DIContainer.CreateTestingContainerBuilder()
                                 ._mutate(it => it.Registrar.CurrentTestsDbPoolIfNotCloneContainer())
                                 .Build();
      return new TestingEndpointHost(rootContainer, ownsRootContainer: true, features);
   }

   ///<summary>Creates a testing host on a root container the test owns and will dispose itself.</summary>
   public static ITestingEndpointHost Create(IDependencyInjectionContainer rootContainer, params IReadOnlyList<ITestingEndpointHostFeature> features) =>
      new TestingEndpointHost(rootContainer, ownsRootContainer: false, features);

   public override IEndpoint RegisterEndpoint(string name, EndpointId id, Action<IEndpointBuilder> setup)
      => base.RegisterEndpoint(name,
                               id,
                               builder =>
                               {
                                  _features.ForEach(feature => feature.SetupEndpoint(builder));
                                  setup(builder);
                               });

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
         foreach(var feature in _features)
         {
            try
            {
               feature.AwaitEndpointsAtRest();
            }
            catch(Exception e)
            {
               unobservedExceptions.Add(e);
            }
         }
      }

      _features.ForEach(feature => unobservedExceptions.AddRange(feature.GetBackgroundExceptions()));

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

      if(unobservedExceptions.Any())
      {
         throw new AggregateException("Unhandled exceptions in testing endpoint host", unobservedExceptions);
      }
   }
#pragma warning restore CA1031

   public async Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest() => await DisposeAsync(true, waitForEndpointsToBeAtRest: false).caf();
}
