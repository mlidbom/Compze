using System.Diagnostics.CodeAnalysis;
using Compze.TypeIdentifiers;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Contracts;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Teventive.Tevents.Public;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;


public static class TessageHandlerRegistryRegistrar
{
   public static IComponentRegistrar TessageHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, ITransactionIgnoringTeventHandlerRegistrar, ITessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((ITypeMap typeMap) => new TessageHandlerRegistry(typeMap)));
}

sealed class TessageHandlerRegistry(ITypeMap typeMap) : ITessageHandlerRegistrar, ITransactionIgnoringTeventHandlerRegistrar, ITessageHandlerRegistry
{
   readonly ITypeMap _typeMap = typeMap;
   IReadOnlyDictionary<Type, Action<object, IUnitOfWorkResolver>> _tommandHandlers = new Dictionary<Type, Action<object, IUnitOfWorkResolver>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>> _teventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>> _transactionIgnoringTeventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>>();
   IReadOnlyList<Type> _registeredTeventTypes = new List<Type>();

   readonly IMonitor _monitor = IMonitor.New();

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler)
   {
      _monitor.Locked(() => AppendTeventSubscription(ref _teventHandlers, handler));
      return this;
   }

   ITransactionIgnoringTeventHandlerRegistrar ITransactionIgnoringTeventHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler)
   {
      _monitor.Locked(() => AppendTeventSubscription(ref _transactionIgnoringTeventHandlers, handler));
      return this;
   }

   ///<summary>The one registration translation, shared by the transactional and the transaction-ignoring (observation) subscription<br/>
   /// kinds. Routing operates exclusively on wrapper types: a subscription to an inner tevent type is keyed under the wrapper type<br/>
   /// matching every wrapping of it and unwrapped at delivery; a subscription to a wrapper type is keyed as it stands and receives<br/>
   /// the wrapper — publisher-conscious subscription. Every subscribed type also joins the advertised set<br/>
   /// (<see cref="HandledRemoteTessageTypeIds"/>): an observation-only subscription must still pull the tevent across the wire.</summary>
   void AppendTeventSubscription<TTevent>(ref IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>> subscriptions, Action<TTevent, IScopeResolver> handler) where TTevent : ITevent
   {
      TessageInspector.AssertValidForSubscription<TTevent>();

      var routingKey = PublisherTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent));
      Action<ITevent, IScopeResolver> deliver = typeof(TTevent).Is<IPublisherTevent<ITevent>>()
                                                   ? (wrappedTevent, kernel) => handler((TTevent)wrappedTevent, kernel)
                                                   : (wrappedTevent, kernel) => handler((TTevent)((IPublisherTevent<ITevent>)wrappedTevent).Tevent, kernel);

      subscriptions.TryGetValue(routingKey, out var currentSubscribers);
      currentSubscribers ??= new List<Action<ITevent, IScopeResolver>>();

      IReadOnlyList<Action<ITevent, IScopeResolver>> value = [..currentSubscribers, deliver];
      Interlocked.Exchange(ref subscriptions, subscriptions.SetInCopy(routingKey, value));
      Interlocked.Exchange(ref _registeredTeventTypes, _registeredTeventTypes.AddToCopy(typeof(TTevent)));
   }

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTommand>();

      if(typeof(TTommand).Implements(typeof(ITommand<>)))
      {
         throw new Exception($"{typeof(TTommand)} expects a result. You must register a method that returns a result.");
      }

      Interlocked.Exchange(ref _tommandHandlers, _tommandHandlers.AddToCopy(typeof(TTommand), Deliver));
      return this;

      void Deliver(object tommand, IUnitOfWorkResolver unitOfWork) => handler((TTommand)tommand, unitOfWork);
   });

   Action<object, IUnitOfWorkResolver> ITessageHandlerRegistry.GetTommandHandler(ITommand tessage) =>
      TryGetTommandHandler(tessage, out var handler) ? handler : throw new NoHandlerException(tessage.GetType());

   bool TryGetTommandHandler(ITommand tessage, [NotNullWhen(true)]out Action<object, IUnitOfWorkResolver>? handler) =>
      _tommandHandlers.TryGetValue(tessage.GetType(), out handler);

   public Action<ITommand, IUnitOfWorkResolver> GetTommandHandler(Type tommandType) => _tommandHandlers[tommandType];

   //performance: Use static caching trick.
   public IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type wrapperTeventType) => [.._teventHandlers.Where(it => it.Key.IsAssignableFrom(wrapperTeventType)).SelectMany(it => it.Value)];

   public IReadOnlyList<Action<ITevent, IScopeResolver>> GetTransactionIgnoringTeventHandlers(Type wrapperTeventType) => [.._transactionIgnoringTeventHandlers.Where(it => it.Key.IsAssignableFrom(wrapperTeventType)).SelectMany(it => it.Value)];

   public ISet<TypeId> HandledRemoteTessageTypeIds()
   {
      //A tevent subscription is advertised in its translated form - the wrapper type matching every wrapping of the subscribed type - because that is the type remote
      //routing matches wrapped tevents against. Advertised = routable: the filter is the exact shape the routers build tevent routes from (wrapper-of-remotable), so a
      //subscription to a type outside it is a purely local subscription, truthfully absent from the advertisement.
      var handledTeventTypes = _registeredTeventTypes
                              .Where(subscribedType => !subscribedType.Implements<TessageTypesInternal.ITessage>())
                              .Select(PublisherTevent.WrapperTypeMatchingAllWrappingsOf)
                              .Where(wrapperType => wrapperType.Is<IPublisherTevent<IRemotableTevent>>());

      var handledTommandTypes = _tommandHandlers.Keys
                                                .Where(IsRemotableNonInfrastructureTessage)
                                                .ToArray();

      //Tessaging routes tommands exactly-once only (the transient tier is a tevent concept - see src/Compze.Tessaging/dev_docs/tevent-delivery-model.md), so any other
      //remotable tommand handler would advertise a type no route can serve: a silently unreachable handler. Every advertised type must get a route - fail loud instead.
      var unroutableTommandTypes = handledTommandTypes.Where(tommandType => !tommandType.Is<IExactlyOnceTommand>()).ToArray();
      State.Assert(unroutableTommandTypes.Length == 0,
                   () => $"These registered remotable tommand handler types would be advertised, but no route can ever serve them - Tessaging routes tommands exactly-once only ({nameof(IExactlyOnceTommand)}): {string.Join(", ", unroutableTommandTypes.Select(it => it.FullName))}.");

      var handledTypes = handledTommandTypes
                        .Concat(handledTeventTypes)
                        .ToHashSet();

      _typeMap.AssertMappingsExistFor(handledTypes);

      return handledTypes.Select(_typeMap.GetId)
                         .ToHashSet();

      static bool IsRemotableNonInfrastructureTessage(Type tessageType) =>
         tessageType.Implements<IRemotableTessage>() && !tessageType.Implements<TessageTypesInternal.ITessage>();
   }

   public IReadOnlyList<Type> RegisteredTypesDemandingExactlyOnceDelivery()
   {
      //Both subscription kinds count: an observation subscription joins the advertisement too, and observing a remote exactly-once tevent still requires receiving it exactly-once.
      var teventTypes = _registeredTeventTypes
                       .Where(subscribedType => PublisherTevent.WrapperTypeMatchingAllWrappingsOf(subscribedType).Is<IPublisherTevent<IExactlyOnceTevent>>());

      var tommandTypes = _tommandHandlers.Keys.Where(tommandType => tommandType.Is<IExactlyOnceTommand>());

      return [..tommandTypes.Concat(teventTypes)];
   }
}
