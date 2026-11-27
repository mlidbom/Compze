using Compze.Tessaging.Engine._private;
using Compze.Tessaging.Validation._internal;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageTypes;
using Compze.Threading;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Engine.HandlerRegistration._private;

///<summary>The handler registrations gathered while an engine is being composed — the mutable precursor its immutable<br/>
/// <see cref="TessageHandlerRoster"/> is built from, covering all four tessage kinds (tevents, tommands, tueries, of either<br/>
/// sibling style) plus tevent observation. Registration ends when <see cref="BuildRoster"/> builds the roster: any registration<br/>
/// after that explodes — the roster is closed, per the engine's contract that its roster can only change by building a new engine.</summary>
///<remarks>Single-handler kinds — tueries and tommands — hold exactly one handler per tessage type: registering a second explodes<br/>
/// immediately, at declaration. Tevent subscriptions are multi-subscriber and accumulate freely.</remarks>
sealed class TessageHandlerRegistrations
{
   readonly IMonitor _monitor = IMonitor.New();
   readonly bool _isTheEngineOfAnEndpoint;
   bool _rosterIsBuilt;

   readonly List<TeventHandlerRegistration> _teventHandlers = [];
   readonly List<TeventObserverRegistration> _teventObservers = [];
   readonly Dictionary<Type, VoidTommandHandlerRegistration> _voidTommandHandlers = new();
   readonly Dictionary<Type, TessageHandlerRoster.TommandHandlerWithResult> _tommandHandlersWithResults = new();
   readonly Dictionary<Type, Func<ITuery, IScopeResolver, Task<object>>> _tueryHandlers = new();
   readonly List<Type> _subscribedTeventTypes = [];
   readonly List<Type> _teventTypesSubscribedForDynamicObservers = [];

   ///<summary>The registrations of a plain container's engine, or, if <paramref name="isTheEngineOfAnEndpoint"/>, of an endpoint's:<br/>
   /// an engine given identity and a wire, which receives tevents from other endpoints.</summary>
   internal TessageHandlerRegistrations(bool isTheEngineOfAnEndpoint) => _isTheEngineOfAnEndpoint = isTheEngineOfAnEndpoint;

   ///<summary>Registers a participation handler for tevents compatible with <typeparamref name="TTevent"/>: see<br/>
   /// <see cref="TeventHandlerRegistration"/>. A tevent runs its handlers in the order they were registered, whatever types they<br/>
   /// subscribe to.</summary>
   internal void AddTeventHandler<TTevent>(Func<TTevent, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted) where TTevent : ITevent => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      _teventHandlers.Add(TeventHandlerRegistration.For(handler, order));
      _subscribedTeventTypes.Add(typeof(TTevent));
   });

   ///<summary>Registers <paramref name="observer"/> — observation, the deliberately transaction-ignoring watch surface: committed<br/>
   /// facts only, dispatched off-thread by the engine's <see cref="TeventObservationDispatcher"/>, never by the transactional<br/>
   /// pipelines. The subscribed type joins the advertised set exactly as a participation subscription does: an observation-only<br/>
   /// subscription must still pull the tevent across the wire.</summary>
   internal void AddTeventObserver(TeventObserverRegistration observer) => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      _teventObservers.Add(observer);
      _subscribedTeventTypes.Add(observer.SubscribedTeventType);
   });

   ///<summary>Subscribes the endpoint to tevents compatible with <paramref name="teventType"/> for its dynamic observers:<br/>
   /// <see cref="Endpoints.EndpointBuilder.SubscribeForDynamicObservers{TTevent}"/>. The type joins the advertised set exactly as<br/>
   /// a composed observer's subscription does, so the endpoint's peers send it such tevents from the start, and a dynamic<br/>
   /// observer of them is in effect the moment it is created.</summary>
   internal void SubscribeForDynamicObservers(Type teventType) => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      _teventTypesSubscribedForDynamicObservers.Add(teventType);
      _subscribedTeventTypes.Add(teventType);
   });

   ///<summary>Registers the handler for <typeparamref name="TTommand"/> — a tommand whose type declares no result. The handler<br/>
   /// receives the <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS: a tommand mutates state, so every path<br/>
   /// that executes one runs it inside a unit of work, and its effects commit or roll back as a whole.</summary>
   internal void AddVoidTommandHandler<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order = HandlingOrder.AsAdmitted) where TTommand : ITommand =>
      _monitor.Locked(() => AddTheVoidTommandHandler(handler, isDeferred: false, order));

   ///<summary>Registers the deferred handler for <typeparamref name="TTommand"/>: one that never runs in its sender's unit of<br/>
   /// work, since the endpoint's inbox runs it in one of its own once the sender has committed — see<br/>
   /// <see cref="TessageBus.DeferredTessageBusHandlerRegistrar"/>. A tommand type has one handler, deferred or not.</summary>
   internal void AddDeferredTommandHandler<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler, HandlingOrder order) where TTommand : IExactlyOnceTommand =>
      _monitor.Locked(() => AddTheVoidTommandHandler(handler, isDeferred: true, order));

   void AddTheVoidTommandHandler<TTommand>(Func<TTommand, IUnitOfWorkResolver, CancellationToken, Task> handler, bool isDeferred, HandlingOrder order) where TTommand : ITommand
   {
      AssertRosterIsNotYetBuilt();
      TessageInspector.AssertValid<TTommand>();
      State.Assert(!typeof(TTommand).Implements(typeof(ITommand<>)),
                   () => $"{typeof(TTommand)} expects a result. You must register a handler that returns its result.");
      AssertSingleHandlerKindIsUnregistered(_voidTommandHandlers, typeof(TTommand));

      _voidTommandHandlers.Add(typeof(TTommand),
                               new VoidTommandHandlerRegistration((tommand, unitOfWork, cancellationToken) => handler((TTommand)tommand, unitOfWork, cancellationToken), isDeferred, order));
   }

   ///<summary>Registers the handler for <typeparamref name="TTommand"/>, whose result answers the caller — see <see cref="AddVoidTommandHandler{TTommand}"/>.</summary>
   internal void AddTommandHandlerWithResult<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, Task<TResult>> handler) where TTommand : ITommand<TResult> => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      TessageInspector.AssertValid<TTommand>();
      AssertSingleHandlerKindIsUnregistered(_tommandHandlersWithResults, typeof(TTommand));

      _tommandHandlersWithResults.Add(typeof(TTommand),
                                      new TessageHandlerRoster.TommandHandlerWithResult(typeof(TResult), async (tommand, unitOfWork) => (await handler((TTommand)tommand, unitOfWork).caf())!));
   });

   ///<summary>Registers the handler for <typeparamref name="TTuery"/>. Tuery handlers receive a plain <see cref="IScopeResolver"/>,<br/>
   /// deliberately: a tuery changes nothing, so its execution is a scope, not a unit of work — no transaction is demanded, and<br/>
   /// when the caller has one the reads simply join its consistency.</summary>
   internal void AddTueryHandler<TTuery, TResult>(Func<TTuery, IScopeResolver, Task<TResult>> handler) where TTuery : ITuery<TResult> => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      TessageInspector.AssertValid<TTuery>();
      AssertSingleHandlerKindIsUnregistered(_tueryHandlers, typeof(TTuery));

      _tueryHandlers.Add(typeof(TTuery), async (tuery, scope) => (await handler((TTuery)tuery, scope).caf())!);
   });

   ///<summary>Builds the immutable <see cref="TessageHandlerRoster"/> from everything registered so far and closes registration:<br/>
   /// the roster is the engine's fixed answer to "what does this component understand", so nothing can join after it is built.<br/>
   /// Several containers may be built from one composition (the testing hosts reuse container wiring); each builds its own<br/>
   /// roster from the same, now-closed registrations.</summary>
   internal TessageHandlerRoster BuildRoster(ITypeMap typeMap) => _monitor.Locked(() =>
   {
      _rosterIsBuilt = true;
      return new TessageHandlerRoster(teventHandlers: [.._teventHandlers],
                                      teventObservers: [.._teventObservers],
                                      isTheEngineOfAnEndpoint: _isTheEngineOfAnEndpoint,
                                      teventTypesSubscribedForDynamicObservers: [.._teventTypesSubscribedForDynamicObservers],
                                      voidTommandHandlers: new Dictionary<Type, VoidTommandHandlerRegistration>(_voidTommandHandlers),
                                      tommandHandlersWithResults: new Dictionary<Type, TessageHandlerRoster.TommandHandlerWithResult>(_tommandHandlersWithResults),
                                      tueryHandlers: new Dictionary<Type, Func<ITuery, IScopeResolver, Task<object>>>(_tueryHandlers),
                                      subscribedTeventTypes: [.._subscribedTeventTypes],
                                      typeMap: typeMap);
   });

   void AssertRosterIsNotYetBuilt() =>
      State.Assert(!_rosterIsBuilt,
                   () => $"The {nameof(TessageHandlerRoster)} is already built: registration closed when the engine was built, and an engine's roster can only change by building a new engine. Register every handler in the composition's declaration block, before the container builds.");

   static void AssertSingleHandlerKindIsUnregistered<THandler>(Dictionary<Type, THandler> handlers, Type tessageType) =>
      State.Assert(!handlers.ContainsKey(tessageType),
                   () => $"A handler for {tessageType.FullName} is already registered. Tueries and tommands are single-handler kinds — the roster holds exactly one handler per such tessage type — so declaring a second handler explodes at declaration.");
}
