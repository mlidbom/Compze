using Compze.Tessaging.Engine.Internal;
using Compze.Tessaging.Validation.Internal;
using Compze.Contracts;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Internals.SystemCE.ThreadingCE.TasksCE;
using Compze.Tessaging.TessageBus;
using Compze.Tessaging.TessageBus.Internal;
using Compze.Tessaging.Internal.Routing;
using Compze.Tessaging.Validation;
using Compze.Tessaging.Engine.HandlerRegistration.TeventObservation;
using Compze.Threading;
using Compze.TypeIdentifiers;

namespace Compze.Tessaging.Engine.HandlerRegistration.Internal;

///<summary>The handler registrations gathered while an engine is being composed — the mutable precursor its immutable<br/>
/// <see cref="TessageHandlerRoster"/> is built from, covering all four tessage kinds (tevents, tommands, tueries, of either<br/>
/// sibling style) plus tevent observation. Registration ends when <see cref="BuildRoster"/> builds the roster: any registration<br/>
/// after that explodes — the roster is closed, per the engine's contract that its roster can only change by building a new engine.</summary>
///<remarks>Single-handler kinds — tueries and tommands — hold exactly one handler per tessage type: registering a second explodes<br/>
/// immediately, at declaration. Tevent subscriptions are multi-subscriber and accumulate freely.</remarks>
sealed class TessageHandlerRegistrations
{
   readonly IMonitor _monitor = IMonitor.New();
   bool _rosterIsBuilt;

   //Tevent subscriptions are keyed by the wrapper routing key their subscribed type translates to - see AddTeventHandler.
   readonly Dictionary<Type, List<Func<ITevent, IUnitOfWorkResolver, Task>>> _teventHandlers = new();
   readonly List<TeventObserverRegistration> _teventObservers = [];
   readonly Dictionary<Type, Func<ITommand, IUnitOfWorkResolver, Task>> _voidTommandHandlers = new();
   readonly Dictionary<Type, TessageHandlerRoster.TommandHandlerWithResult> _tommandHandlersWithResults = new();
   readonly Dictionary<Type, Func<ITuery, IScopeResolver, Task<object>>> _tueryHandlers = new();
   readonly List<Type> _subscribedTeventTypes = [];

   ///<summary>Registers a participation handler for tevents compatible with <typeparamref name="TTevent"/>. The one registration<br/>
   /// translation of the routing model: routing operates exclusively on wrapper types, so a subscription to an inner tevent type<br/>
   /// is keyed under the wrapper type matching every wrapping of it (<see cref="PublisherTevent.WrapperTypeMatchingAllWrappingsOf"/>)<br/>
   /// and unwrapped at delivery, while a subscription to a wrapper type is keyed as it stands and receives the wrapper —<br/>
   /// publisher-conscious subscription.</summary>
   internal void AddTeventHandler<TTevent>(Func<TTevent, IUnitOfWorkResolver, Task> handler) where TTevent : ITevent => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      TessageInspector.AssertValidForSubscription<TTevent>();

      Func<ITevent, IUnitOfWorkResolver, Task> deliver = typeof(TTevent).Is<IPublisherTevent<ITevent>>()
                                                            ? (wrappedTevent, unitOfWork) => handler((TTevent)wrappedTevent, unitOfWork)
                                                            : (wrappedTevent, unitOfWork) => handler((TTevent)((IPublisherTevent<ITevent>)wrappedTevent).Tevent, unitOfWork);

      _teventHandlers.GetOrAdd(PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent)), () => []).Add(deliver);
      _subscribedTeventTypes.Add(typeof(TTevent));
   });

   ///<summary>Registers an observer for tevents compatible with <typeparamref name="TTevent"/> — observation, the deliberately<br/>
   /// transaction-ignoring watch surface: committed facts only, dispatched off-thread by the engine's<br/>
   /// <see cref="TeventObservationDispatcher"/>, never by the transactional pipelines. The same registration translation as<br/>
   /// <see cref="AddTeventHandler{TTevent}"/> applies, and the subscribed type joins the advertised set exactly as a<br/>
   /// participation subscription does: an observation-only subscription must still pull the tevent across the wire.</summary>
   internal void AddTeventObserver<TTevent>(Action<TTevent, IScopeResolver> observer) where TTevent : ITevent => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      TessageInspector.AssertValidForSubscription<TTevent>();

      Action<ITevent, IScopeResolver> deliver = typeof(TTevent).Is<IPublisherTevent<ITevent>>()
                                                   ? (wrappedTevent, scope) => observer((TTevent)wrappedTevent, scope)
                                                   : (wrappedTevent, scope) => observer((TTevent)((IPublisherTevent<ITevent>)wrappedTevent).Tevent, scope);

      _teventObservers.Add(new TeventObserverRegistration(PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent)), deliver));
      _subscribedTeventTypes.Add(typeof(TTevent));
   });

   ///<summary>Registers the handler for <typeparamref name="TTommand"/> — a tommand whose type declares no result. The handler<br/>
   /// receives the <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS: a tommand mutates state, so every path<br/>
   /// that executes one runs it inside a unit of work, and its effects commit or roll back as a whole.</summary>
   internal void AddVoidTommandHandler<TTommand>(Func<TTommand, IUnitOfWorkResolver, Task> handler) where TTommand : ITommand => _monitor.Locked(() =>
   {
      AssertRosterIsNotYetBuilt();
      TessageInspector.AssertValid<TTommand>();
      State.Assert(!typeof(TTommand).Implements(typeof(ITommand<>)),
                   () => $"{typeof(TTommand)} expects a result. You must register a handler that returns its result.");
      AssertSingleHandlerKindIsUnregistered(_voidTommandHandlers, typeof(TTommand));

      _voidTommandHandlers.Add(typeof(TTommand), (tommand, unitOfWork) => handler((TTommand)tommand, unitOfWork));
   });

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
      return new TessageHandlerRoster(teventHandlers: _teventHandlers.ToDictionary(it => it.Key, IReadOnlyList<Func<ITevent, IUnitOfWorkResolver, Task>> (it) => [..it.Value]),
                                      teventObservers: [.._teventObservers],
                                      voidTommandHandlers: new Dictionary<Type, Func<ITommand, IUnitOfWorkResolver, Task>>(_voidTommandHandlers),
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
