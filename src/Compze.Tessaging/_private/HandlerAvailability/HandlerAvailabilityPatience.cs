namespace Compze.Tessaging._private.HandlerAvailability;

///<summary>How long the endpoint waits for a handler to become available before failing loud — the bound on every waiting<br/>
/// send (see <see cref="IHandlerAvailability"/>). One flat patience, deliberately: differentiating it (shorter for never-seen<br/>
/// types, longer for known-but-down peers) would add a knob no real need has asked for — the known-but-down vs never-seen<br/>
/// distinction lives in the failure wording instead (see<br/>
/// <c>src/Compze.Tessaging/dev_docs/peer-model.md</c>).</summary>
class HandlerAvailabilityPatience
{
   ///<summary>The flat 30-second default every endpoint gets unless its composition declares otherwise<br/>
   /// (<see cref="Endpoints.EndpointBuilder{TConcreteBuilder}.HandlerAvailabilityPatience"/>).</summary>
   internal static readonly HandlerAvailabilityPatience Default = new(TimeSpan.FromSeconds(30));

   internal TimeSpan Duration { get; }

   internal HandlerAvailabilityPatience(TimeSpan duration) => Duration = duration;
}
