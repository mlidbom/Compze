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
    ///<summary>The registry every endpoint in the host participates in: each announces its address here and discovers the others<br/>
    /// through it — the same announce/discover pipeline a production same-machine suite runs, backed by a real interprocess<br/>
    /// registry the host owns (created per host, deleted when the host is disposed).</summary>
    IEndpointRegistryAndAnnouncer EndpointRegistry { get; }

    ///<summary>Disposes without first waiting for the endpoints to come to rest — for tests that deliberately leave work in flight, such as scheduled tessages that must never be delivered.</summary>
    Task DisposeAsyncWithoutWaitingForEndpointsToBeAtRest();
}
