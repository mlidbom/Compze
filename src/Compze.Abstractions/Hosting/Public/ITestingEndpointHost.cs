namespace Compze.Abstractions.Hosting.Public;

///<summary>
/// An <see cref="IEndpointHost"/> for tests. Beyond hosting endpoints, a testing host guards test correctness
/// at dispose: it waits until its endpoints are at rest (no tracked background work, such as tessages, remains
/// in flight) and rethrows any exceptions background work produced that no assertion observed — so a test
/// cannot pass while having silently dropped work or swallowed failures.
///
/// Created via <c>TestingEndpointHost.Create</c> (in Compze.Hosting.Testing), passing the testing features
/// the test needs — Tessaging's, Typermedia's, or both.
///</summary>
public interface ITestingEndpointHost : IEndpointHost
{
    ///<summary>Disposes without first waiting for the endpoints to come to rest — for tests that deliberately leave work in flight, such as scheduled tessages that must never be delivered.</summary>
    Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
}
