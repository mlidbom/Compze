using Compze.Abstractions.Hosting.Public;

namespace Compze.Hosting.Testing;

///<summary>
/// What a capability — typically a paradigm such as Tessaging or Typermedia — contributes to a
/// <see cref="TestingEndpointHost"/>: standard test wiring added to every endpoint the host registers, and
/// participation in the host's dispose-time quiescence wait and background-failure reporting.
///
/// This mirrors, one level up, how paradigms plug their pipelines into individual endpoints via
/// <see cref="IEndpointBuilder.GetOrAddFeature{TFeature}"/>: the testing host knows no paradigm; whichever
/// features it is created with decide what every test endpoint is wired with.
///</summary>
public interface ITestingEndpointHostFeature
{
   ///<summary>Called once, when the host is created with this feature, before any endpoint is registered. Lets the feature hold on to the host — for example to read <see cref="IEndpointHost.Endpoints"/> lazily.</summary>
   void OnAddedToHost(ITestingEndpointHost host) {}

   ///<summary>Called for every endpoint registered with the host, before the test's own setup runs. Registers the feature's standard test wiring so individual tests don't repeat it.</summary>
   void SetupEndpoint(IEndpointBuilder builder);

   ///<summary>Blocks until background work this feature tracks has come to rest host-wide. Called when the host is disposed — unless the test opted out via <see cref="ITestingEndpointHost.DisposeAsyncWithoutWaitingForEndpointsToBeAtRest"/> — so tests fail rather than silently drop in-flight work.</summary>
   void AwaitEndpointsAtRest() {}

   ///<summary>Exceptions from background work that no test assertion observed. The host rethrows them when disposed so background failures cannot pass silently.</summary>
   IReadOnlyList<Exception> GetBackgroundExceptions() => [];
}
