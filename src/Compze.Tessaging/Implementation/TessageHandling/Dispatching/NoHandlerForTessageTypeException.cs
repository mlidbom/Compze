using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

///<summary>Thrown when a tommand is sent whose type nothing known handles: neither this endpoint's own handlers nor any<br/>
/// remembered peer's advertisement (see <see cref="Peers.IPeerRegistry"/>). A tommand routes at delivery time to whichever<br/>
/// endpoint advertises its type, but an endpoint advertising it must have been met at least once — first contact is the<br/>
/// boundary (see <c>dev_docs/TODO/durable-peer-topology.md</c>).</summary>
class NoHandlerForTessageTypeException(Type tommandType) : Exception(
   $"Nothing handles the tommand type {tommandType.GetFullNameCompilable()}: neither this endpoint's own handlers nor any remembered peer's advertisement. An endpoint that handles it must have been met at least once before sends can route to it.");
