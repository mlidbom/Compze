namespace Compze.Abstractions.Hosting.Public;

///<summary>A registry with both faces of endpoint discovery: the <see cref="IEndpointRegistry"/> read side through which an<br/>
/// endpoint discovers the others, and the <see cref="IEndpointAddressAnnouncer"/> write side through which it announces itself —<br/>
/// what an endpoint participates in (<c>ParticipateIn(...)</c> on a distributed communication style's feature), each member both<br/>
/// finding the others and being found by them. The same-machine suite's interprocess registry is the canonical implementation,<br/>
/// and the testing host runs every test's endpoints on one.</summary>
public interface IEndpointRegistryAndAnnouncer : IEndpointRegistry, IEndpointAddressAnnouncer;
