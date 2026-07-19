using Compze.Tessaging.Endpoints;
using Compze.Internals.SystemCE.ReflectionCE;

namespace Compze.Tessaging.Typermedia.Client;

///<summary>Thrown when a typermedia tessage's type has no handler anywhere the sender can reach. For an endpoint navigating<br/>
/// remote typermedia this is exclusively the patience-exhausted failure: the send first waited, bounded, for a route to appear<br/>
/// (see <c>IHandlerAvailability</c>), and the message tells known-but-down — a remembered peer serves the type and is down —<br/>
/// from never-seen: nothing this endpoint has ever met serves it, almost certainly a deployment or configuration error. An<br/>
/// external client application's router throws it immediately instead: the client connects explicitly to every endpoint it<br/>
/// navigates, so there is no discovery to wait for. Public because it reaches client code, which must be able to catch it.</summary>
public class NoHandlerForTypermediaTypeException : Exception
{
   NoHandlerForTypermediaTypeException(string message) : base(message) {}

   internal static NoHandlerForTypermediaTypeException BecausePatienceIsExhausted(Type tessageType, TimeSpan patience, IReadOnlyList<EndpointId> rememberedHandlerIds) => new(
      $"No connected endpoint advertises a handler for the typermedia type {tessageType.GetFullNameCompilable()}, and none appeared within the endpoint's handler-availability patience (waited {patience.TotalSeconds:0.###}s). "
      + (rememberedHandlerIds.Count == 0
            ? "Nothing this endpoint has ever met serves the type: almost certainly a deployment or configuration error — the endpoint serving it is not deployed, does not announce to this endpoint's registry, or does not advertise the type."
            : $"Remembered peers whose last-known advertisement serves it: {string.Join(", ", rememberedHandlerIds)} — the handler peer is known and currently down, and did not return in time."));

   internal static NoHandlerForTypermediaTypeException BecauseTheClientIsConnectedToNoHandler(Type tessageType) => new(
      $"No connected endpoint advertises a handler for the typermedia type {tessageType.GetFullNameCompilable()}. An external client application connects explicitly to every endpoint it navigates: connect to the endpoint serving this type before navigating it.");
}
