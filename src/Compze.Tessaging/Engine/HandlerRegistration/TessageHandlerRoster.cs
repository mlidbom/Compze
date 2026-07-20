using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.Engine.Exceptions;
using Compze.Tessaging.Engine.HandlerRegistration.TessageHandlers;
using Compze.Tessaging.Engine.HandlerRegistration.TeventObservation;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Engine.HandlerRegistration;

///<summary>The closed set of what one engine understands: an immutable map from tessage type to handler covering all four<br/>
/// tessage kinds — tevents and tommands of the TessageBus style, tueries and tommands of the Typermedia style — plus tevent<br/>
/// observation. Produced from the composition's gathered <see cref="TessageHandlerRegistrations"/> when the engine is built and<br/>
/// never modified afterward: the roster can only change by building a new engine, which is what a process restart does.</summary>
///<remarks>Tevent handlers are multi-subscriber: a published tevent reaches every handler whose subscribed type it is compatible<br/>
/// with, through the type hierarchy. Tueries and tommands have exactly one handler each — a missing one is the<br/>
/// <see cref="NoHandlerException"/> on every lookup path, never a raw dictionary failure. The roster is also the single source<br/>
/// of truth for the engine's advertisement (<see cref="AdvertisedRemoteTessageTypeIds"/>): the remotable types it serves, as<br/>
/// canonical type-ids, computed once.</remarks>
public class TessageHandlerRoster
{
   readonly IReadOnlyDictionary<Type, IReadOnlyList<Func<ITevent, IUnitOfWorkResolver, Task>>> _teventHandlers;
   readonly IReadOnlyDictionary<Type, Func<ITommand, IUnitOfWorkResolver, Task>> _voidTommandHandlers;
   readonly IReadOnlyDictionary<Type, TommandHandlerWithResult> _tommandHandlersWithResults;
   readonly IReadOnlyDictionary<Type, Func<ITuery, IScopeResolver, Task<object>>> _tueryHandlers;
   readonly IReadOnlyList<Type> _subscribedTeventTypes;
   readonly ITypeMap _typeMap;

   readonly Lazy<ISet<TypeId>> _handledRemoteTessageBusTypeIds;
   readonly Lazy<ISet<TypeId>> _handledRemoteTypermediaTypeIds;

   internal TessageHandlerRoster(IReadOnlyDictionary<Type, IReadOnlyList<Func<ITevent, IUnitOfWorkResolver, Task>>> teventHandlers,
                                 IReadOnlyList<TeventObserverRegistration> teventObservers,
                                 IReadOnlyDictionary<Type, Func<ITommand, IUnitOfWorkResolver, Task>> voidTommandHandlers,
                                 IReadOnlyDictionary<Type, TommandHandlerWithResult> tommandHandlersWithResults,
                                 IReadOnlyDictionary<Type, Func<ITuery, IScopeResolver, Task<object>>> tueryHandlers,
                                 IReadOnlyList<Type> subscribedTeventTypes,
                                 ITypeMap typeMap)
   {
      _teventHandlers = teventHandlers;
      TeventObserverRegistrations = teventObservers;
      _voidTommandHandlers = voidTommandHandlers;
      _tommandHandlersWithResults = tommandHandlersWithResults;
      _tueryHandlers = tueryHandlers;
      _subscribedTeventTypes = subscribedTeventTypes;
      _typeMap = typeMap;
      //Computed once, lazily: the projections assert that every advertised type has a type-id mapping, and a strictly-local
      //composition - which never advertises - must not be forced to map types it never sends anywhere.
      _handledRemoteTessageBusTypeIds = new Lazy<ISet<TypeId>>(ComputeHandledRemoteTessageBusTypeIds);
      _handledRemoteTypermediaTypeIds = new Lazy<ISet<TypeId>>(ComputeHandledRemoteTypermediaTypeIds);
   }

   ///<summary>The participation tevent handlers whose subscriptions match <paramref name="wrapperTeventType"/>. Every dispatch<br/>
   /// site invokes them inside a unit of work — the publisher's own for a local publish, the inbox processing's own for an<br/>
   /// exactly-once arrival, the direct dispatch's own for a best-effort arrival.</summary>
   //performance: Use static caching trick.
   public IReadOnlyList<Func<ITevent, IUnitOfWorkResolver, Task>> GetTeventHandlers(Type wrapperTeventType) =>
      [.._teventHandlers.Where(it => it.Key.IsAssignableFrom(wrapperTeventType)).SelectMany(it => it.Value)];

   ///<summary>Every declared tevent observer with its subscribed wrapper type — observation, the deliberately<br/>
   /// transaction-ignoring watch surface. The engine's <see cref="TeventObservationDispatcher"/> builds its per-observer FIFO<br/>
   /// dispatch queues from these; the transactional pipelines never touch them.</summary>
   internal IReadOnlyList<TeventObserverRegistration> TeventObserverRegistrations { get; }

   ///<summary>The one handler for the tommand type <paramref name="tommandType"/>, whose type declares no result.</summary>
   public Func<ITommand, IUnitOfWorkResolver, Task> GetVoidTommandHandler(Type tommandType) =>
      _voidTommandHandlers.TryGetValue(tommandType, out var handler) ? handler : throw new NoHandlerException(tommandType);

   ///<summary>The one handler for the tommand type <paramref name="tommandType"/>, whose result answers the caller.</summary>
   public Func<ITommand, IUnitOfWorkResolver, Task<object>> GetTommandHandlerWithResult(Type tommandType) =>
      _tommandHandlersWithResults.TryGetValue(tommandType, out var handler) ? handler.Handle : throw new NoHandlerException(tommandType);

   ///<summary>The one handler for the tuery type <paramref name="tueryType"/>.</summary>
   public Func<ITuery, IScopeResolver, Task<object>> GetTueryHandler(Type tueryType) =>
      _tueryHandlers.TryGetValue(tueryType, out var handler) ? handler : throw new NoHandlerException(tueryType);

   ///<summary>Whether this roster holds a handler for the tommand type <paramref name="tommandType"/> — the question the<br/>
   /// tommand-sender door asks to honor the consistency law: an in-roster tommand executes inline, in the sender's execution,<br/>
   /// and only a tommand whose handler lives elsewhere crosses the endpoint boundary through delivery machinery.</summary>
   internal bool HandlesTommand(Type tommandType) => _voidTommandHandlers.ContainsKey(tommandType) || _tommandHandlersWithResults.ContainsKey(tommandType);

   ///<summary>Whether this roster holds a handler for the single-handler tessage type <paramref name="tessageType"/> — a<br/>
   /// tommand of either shape, or a tuery. What readiness asks about the endpoint's own half: a type the endpoint itself<br/>
   /// serves is reachable in-boundary — inline for tommands, the strictly-local navigator for typermedia — needing no<br/>
   /// discovery and no route (see <c>IHandlerAvailability</c>).</summary>
   internal bool HandlesTheSingleHandlerType(Type tessageType) => HandlesTommand(tessageType) || _tueryHandlers.ContainsKey(tessageType);

   ///<summary>The engine's advertisement: the <see cref="TypeId"/> of every remotable, non-infrastructure tessage type this roster<br/>
   /// serves, of every kind, in the form remote routing matches against — tevent subscriptions as their translated wrapper types,<br/>
   /// tommands and tueries as they stand. Computed once; asserts that every advertised type has a type-id mapping.</summary>
   public ISet<TypeId> AdvertisedRemoteTessageTypeIds() => _handledRemoteTessageBusTypeIds.Value.Union(_handledRemoteTypermediaTypeIds.Value).ToHashSet();

   ///<summary>The registered handler tessage types whose type declares the exactly-once delivery contract — the types an engine's<br/>
   /// endpoint may only advertise when its composition wires the exactly-once machinery (the inbox that persists, dedups, and<br/>
   /// retries). Observation subscriptions count too: observing a remote exactly-once tevent still requires receiving it<br/>
   /// exactly-once. See <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>.</summary>
   internal IReadOnlyList<Type> RegisteredTypesDemandingExactlyOnceDelivery()
   {
      //Both subscription kinds count: an observation subscription joins the advertisement too, and observing a remote exactly-once tevent still requires receiving it exactly-once.
      var teventTypes = _subscribedTeventTypes
                       .Where(subscribedType => PublisherTevent.WrapperTypeMatchingAllWrappingsOf(subscribedType).Is<IPublisherTevent<IExactlyOnceTevent>>());

      var tommandTypes = _voidTommandHandlers.Keys.Where(tommandType => tommandType.Is<IExactlyOnceTommand>());

      return [..tommandTypes.Concat(teventTypes)];
   }

   ISet<TypeId> ComputeHandledRemoteTessageBusTypeIds()
   {
      //A tevent subscription is advertised in its translated form - the wrapper type matching every wrapping of the subscribed type - because that is the type remote
      //routing matches wrapped tevents against. Advertised = routable: the filter is the exact shape the routers build tevent routes from (wrapper-of-remotable), so a
      //subscription to a type outside it is a purely local subscription, truthfully absent from the advertisement.
      var handledTeventTypes = _subscribedTeventTypes
                              .Select(PublisherTevent.WrapperTypeMatchingAllWrappingsOf)
                              .Where(wrapperType => wrapperType.Is<IPublisherTevent<IRemotableTevent>>());

      //The TessageBus tommand partition is the exactly-once tommands; typermedia tommands are the typermedia partition's. A remotable
      //tommand handler of NEITHER kind would advertise a type no route can ever serve - a silently unreachable handler. Every
      //advertised type must get a route - fail loud instead.
      var remotableVoidTommandTypes = _voidTommandHandlers.Keys
                                                          .Where(tommandType => tommandType.Implements<IRemotableTessage>())
                                                          .ToArray();
      var handledTommandTypes = remotableVoidTommandTypes.Where(tommandType => tommandType.Is<IExactlyOnceTommand>()).ToArray();

      var unroutableTommandTypes = remotableVoidTommandTypes.Where(tommandType => !tommandType.Is<IExactlyOnceTommand>() && !tommandType.Is<IAtMostOnceTypermediaTommand>()).ToArray();
      State.Assert(unroutableTommandTypes.Length == 0,
                   () => $"These registered remotable tommand handler types would be advertised, but no route can ever serve them - a remotable tommand is either exactly-once TessageBus ({nameof(IExactlyOnceTommand)}) or at-most-once typermedia ({nameof(IAtMostOnceTypermediaTommand)}): {string.Join(", ", unroutableTommandTypes.Select(it => it.FullName))}.");

      var handledTypes = handledTommandTypes
                        .Concat(handledTeventTypes)
                        .ToHashSet();

      _typeMap.AssertMappingsExistFor(handledTypes);

      return handledTypes.Select(_typeMap.GetId)
                         .ToHashSet();
   }

   ISet<TypeId> ComputeHandledRemoteTypermediaTypeIds()
   {
      var handledTypes = _tommandHandlersWithResults.Keys
                                                    .Concat(_tueryHandlers.Keys)
                                                    .Concat(_voidTommandHandlers.Keys.Where(tommandType => tommandType.Is<IAtMostOnceTypermediaTommand>()))
                                                    .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                                    .ToHashSet();

      var remoteResultTypes = _tommandHandlersWithResults
                             .Where(handler => handler.Key.Implements<IRemotableTessage>())
                             .Where(handler => handler.Value.ResultType.Implements<IRemotableTessage>())
                             .Select(handler => handler.Value.ResultType)
                             .ToList();

      var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

      _typeMap.AssertMappingsExistFor(typesNeedingMappings);

      return handledTypes.Select(_typeMap.GetId)
                         .ToHashSet();
   }

   //A named registration rather than a bare tuple: the advertisement needs the result type - a remotable tommand's remotable result must have a type-id mapping too.
   internal class TommandHandlerWithResult(Type resultType, Func<ITommand, IUnitOfWorkResolver, Task<object>> handle)
   {
      internal Type ResultType { get; } = resultType;
      internal Func<ITommand, IUnitOfWorkResolver, Task<object>> Handle { get; } = handle;
   }
}
