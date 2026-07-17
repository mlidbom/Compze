using Compze.Abstractions.Hosting.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Implementation.Peers;

///<summary>One peer as this endpoint remembers it: the peer's identity (<see cref="EndpointId"/>) and its last-known<br/>
/// advertisement, with the advertised type strings resolved to types once, when the peer is remembered — so<br/>
/// <see cref="SubscribesTo"/> and <see cref="Handles"/> are pure type checks on every publish and send. The advertisement<br/>
/// partitions the way the router's route registration partitions it: tevent subscriptions are wrapper types matched by<br/>
/// assignability, everything else is a tommand type matched exactly — so the registry and the routes always agree.</summary>
public class RememberedPeer
{
   ///<summary>The peer's identity — stable across restarts and address changes.</summary>
   public EndpointId Id { get; }

   ///<summary>The peer's last-known advertisement: the canonical type-id strings of every remotable tessage type it handles and subscribes to.</summary>
   public IReadOnlySet<string> HandledTessageTypes { get; }

   readonly IReadOnlyList<Type> _teventSubscriptions;
   readonly HashSet<Type> _handledTommandTypes;

   internal RememberedPeer(EndpointId id, IReadOnlySet<string> handledTessageTypes, ITypeMap typeMap)
   {
      Id = id;
      HandledTessageTypes = handledTessageTypes;
      var advertisedTypes = handledTessageTypes.Select(typeIdString => typeMap.GetId(typeIdString).Type).ToList();
      _teventSubscriptions = [..advertisedTypes.Where(advertisedType => advertisedType.Is<ITevent>())];
      _handledTommandTypes = [..advertisedTypes.Where(advertisedType => !advertisedType.Is<ITevent>())];
   }

   ///<summary>Whether this peer's last-known advertisement subscribes to <paramref name="wrappedTevent"/> — the same<br/>
   /// advertised-wrapper-type assignability test the router's routes apply.</summary>
   internal bool SubscribesTo(IPublisherTevent<IRemotableTevent> wrappedTevent) => SubscribesToTeventsOf(wrappedTevent.GetType());

   ///<summary>The type-level form of <see cref="SubscribesTo"/>, for tevents at rest — an outbox row carries the published<br/>
   /// wrapper's type, not an instance — asking the same question: does any advertised subscription match this wrapper type?</summary>
   internal bool SubscribesToTeventsOf(Type publishedWrapperType)
      => _teventSubscriptions.Any(subscription => subscription.IsAssignableFrom(publishedWrapperType));

   ///<summary>Whether this peer's last-known advertisement handles <paramref name="tommand"/>'s type — the same exact-type<br/>
   /// match the router's tommand routes apply.</summary>
   internal bool Handles(IExactlyOnceTommand tommand) => HandlesTommandsOf(tommand.GetType());

   ///<summary>The type-level form of <see cref="Handles"/>, for tommands at rest — an outbox row carries the tommand's type,<br/>
   /// not an instance.</summary>
   internal bool HandlesTommandsOf(Type tommandType) => _handledTommandTypes.Contains(tommandType);
}
