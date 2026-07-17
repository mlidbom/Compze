using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;

///<summary>Thrown when a tommand is sent whose type nothing known handles: neither this endpoint's own handlers nor any<br/>
/// remembered peer's advertisement (see <see cref="Peers.IPeerRegistry"/>). A tommand binds to a specific receiver at send<br/>
/// time, and an endpoint handling the type must have been met at least once to be bound to — first contact is the boundary<br/>
/// (see <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>). Exclusively the patience-exhausted failure: the send<br/>
/// first waited, bounded by the endpoint's handler-availability patience, for that first contact<br/>
/// (see <c>IHandlerAvailability</c>). Public because it reaches the sending application code, which must be able to catch it.</summary>
public class NoHandlerForTessageTypeException : Exception
{
   NoHandlerForTessageTypeException(string message) : base(message) {}

   internal static NoHandlerForTessageTypeException BecausePatienceIsExhausted(Type tommandType, TimeSpan patience) => new(
      $"Nothing handles the tommand type {tommandType.GetFullNameCompilable()}: neither this endpoint's own handlers nor any remembered peer's advertisement, and no first contact with an endpoint handling it occurred within the endpoint's handler-availability patience (waited {patience.TotalSeconds:0.###}s). "
      + "An endpoint that handles the type must be met at least once before sends can bind to it: almost certainly a deployment or configuration error — the handling endpoint is not deployed, does not announce to this endpoint's registry, or does not advertise the type.");
}
