using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.Endpoints;
using Compze.Tessaging.TessageTypes;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Peers;

///<summary>One peer as this endpoint remembers it: the peer's identity (<see cref="EndpointId"/>) and its last-known<br/>
/// advertisement, with the advertised type strings resolved to types once, when the peer is remembered — so<br/>
/// <see cref="SubscribesTo"/> and <see cref="Handles"/> are pure type checks on every publish and send. The advertisement<br/>
/// partitions the way the router's route registration partitions it: tevent subscriptions are wrapper types matched by<br/>
/// assignability; the remotable single-handler types — exactly-once tommands, typermedia tommands, tueries — are matched<br/>
/// exactly — so the registry and the routes always agree. The single-handler memory serves two askers: send-time receiver<br/>
/// binding for exactly-once tommands, and the known-but-down vs never-seen distinction waiting sends and readiness compute<br/>
/// their availability and failure wording from.</summary>
public class RememberedPeer
{
   ///<summary>The peer's identity — stable across restarts and address changes.</summary>
   public EndpointId Id { get; }

   ///<summary>The peer's last-known advertisement: the canonical type-id strings of every remotable tessage type it serves, of every kind.</summary>
   public IReadOnlySet<string> HandledTessageTypes { get; }

   readonly IReadOnlyList<Type> _teventSubscriptions;
   readonly HashSet<Type> _handledSingleHandlerTypes;

   internal RememberedPeer(EndpointId id, IReadOnlySet<string> handledTessageTypes, ITypeMap typeMap)
   {
      Id = id;
      HandledTessageTypes = handledTessageTypes;
      //bug: GetId throws when this endpoint can't map one of the peer's advertised types, so this endpoint never remembers that
      //   peer: each reconciliation pass fails the same way, and the only sign is a warning in the log. RememberedPeers.SubscriberIdsFor
      //   then leaves the peer out, so the tevents this endpoint publishes that the peer subscribes to get no outbox row and never
      //   reach it, with no exception anywhere. Sends to the peer wait out the handler-availability patience and fail with
      //   NoHandlerForTessageTypeException, whose message blames the deployment. So every endpoint must map every type of every peer
      //   it converses with, even types it never sends or publishes. Skipping what it can't map is not a fix: a tevent subscription is
      //   matched by assignability, so a skipped one could be a supertype of a tevent this endpoint publishes, and that tevent would
      //   silently not be delivered. The fix must guarantee an exception wherever routing could be wrong (Magnus, 2026-11-24); the
      //   designs are put to him. (website-and-docs and compze-agent-rep, 2026-11-24)
      var advertisedTypes = handledTessageTypes.Select(typeIdString => typeMap.GetId(typeIdString).Type).ToList();
      _teventSubscriptions = [..advertisedTypes.Where(advertisedType => advertisedType.Is<ITevent>())];
      _handledSingleHandlerTypes = [..advertisedTypes.Where(advertisedType => !advertisedType.Is<ITevent>())];
   }

   ///<summary>Whether this peer's last-known advertisement subscribes to <paramref name="wrappedTevent"/> — the same<br/>
   /// advertised-wrapper-type assignability test the router's routes apply.</summary>
   internal bool SubscribesTo(IPublisherTevent<IRemotableTevent> wrappedTevent) => SubscribesToTeventsOf(wrappedTevent.GetType());

   ///<summary>The type-level form of <see cref="SubscribesTo"/>, for tevents at rest — an outbox row carries the published<br/>
   /// wrapper's type, not an instance — asking the same question: does any advertised subscription match this wrapper type?</summary>
   internal bool SubscribesToTeventsOf(Type publishedWrapperType)
      => _teventSubscriptions.Any(subscription => subscription.IsAssignableFrom(publishedWrapperType));

   ///<summary>Whether this peer's last-known advertisement handles the remotable single-handler type<br/>
   /// <paramref name="tessageType"/> — an exactly-once tommand, a typermedia tommand, or a tuery — the same exact-type match<br/>
   /// the router's routes apply to these kinds. (Tevent subscriptions are a different question: <see cref="SubscribesTo"/>.)</summary>
   internal bool Handles(Type tessageType) => _handledSingleHandlerTypes.Contains(tessageType);
}
