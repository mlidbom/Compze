using System.Diagnostics.CodeAnalysis;
using Compze.TypeIdentifiers;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
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
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, ITessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((ITypeMap typeMap) => new TessageHandlerRegistry(typeMap)));
}

sealed class TessageHandlerRegistry(ITypeMap typeMap) : ITessageHandlerRegistrar, ITessageHandlerRegistry
{
   readonly ITypeMap _typeMap = typeMap;
   IReadOnlyDictionary<Type, Action<object, IScopeResolver>> _tommandHandlers = new Dictionary<Type, Action<object, IScopeResolver>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>> _teventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>>();
   IReadOnlyList<Type> _registeredTeventTypes = new List<Type>();

   readonly IMonitor _monitor = IMonitor.New();

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValidForSubscription<TTevent>();

      //Routing operates exclusively on wrapper types: a subscription to an inner tevent type is keyed under the wrapper type matching every wrapping of it
      //and unwrapped at delivery; a subscription to a wrapper type is keyed as it stands and receives the wrapper - publisher-conscious subscription.
      var routingKey = PublisherIdentifyingTevent.WrapperTypeMatchingAllWrappingsOf(typeof(TTevent));
      Action<ITevent, IScopeResolver> deliver = typeof(TTevent).Is<IPublisherIdentifyingTevent<ITevent>>()
                                                   ? (wrappedTevent, kernel) => handler((TTevent)wrappedTevent, kernel)
                                                   : (wrappedTevent, kernel) => handler((TTevent)((IPublisherIdentifyingTevent<ITevent>)wrappedTevent).Tevent, kernel);

      _teventHandlers.TryGetValue(routingKey, out var currentTeventSubscribers);
      currentTeventSubscribers ??= new List<Action<ITevent, IScopeResolver>>();

      IReadOnlyList<Action<ITevent, IScopeResolver>> value = [..currentTeventSubscribers, deliver];
      Interlocked.Exchange(ref _teventHandlers, _teventHandlers.SetInCopy(routingKey, value));
      Interlocked.Exchange(ref _registeredTeventTypes, _registeredTeventTypes.AddToCopy(typeof(TTevent)));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTommand<TTommand>(Action<TTommand, IScopeResolver> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTommand>();

      if(typeof(TTommand).Implements(typeof(ITommand<>)))
      {
         throw new Exception($"{typeof(TTommand)} expects a result. You must register a method that returns a result.");
      }

      void Value(object tommand, IScopeResolver kernel) => handler((TTommand)tommand, kernel);
      Interlocked.Exchange(ref _tommandHandlers, _tommandHandlers.AddToCopy(typeof(TTommand), Value));
      return this;
   });

   Action<object, IScopeResolver> ITessageHandlerRegistry.GetTommandHandler(ITommand tessage)
   {
      if(TryGetTommandHandler(tessage, out var handler)) return handler;

      throw new NoHandlerException(tessage.GetType());
   }

   bool TryGetTommandHandler(ITommand tessage, [NotNullWhen(true)]out Action<object, IScopeResolver>? handler) =>
      _tommandHandlers.TryGetValue(tessage.GetType(), out handler);

   public Action<ITommand, IScopeResolver> GetTommandHandler(Type tommandType) => _tommandHandlers[tommandType];

   //performance: Use static caching trick.
   public IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type wrapperTeventType) => _teventHandlers.Where(it => it.Key.IsAssignableFrom(wrapperTeventType)).SelectMany(it => it.Value).ToList();

   public ISet<TypeId> HandledRemoteTessageTypeIds()
   {
      //A tevent subscription is advertised in its translated form - the wrapper type matching every wrapping of the subscribed type - because that is the type remote routing matches wrapped tevents against.
      var handledTeventTypes = _registeredTeventTypes
                              .Where(IsRemotableNonInfrastructureTessage)
                              .Select(PublisherIdentifyingTevent.WrapperTypeMatchingAllWrappingsOf);

      var handledTypes = _tommandHandlers.Keys
                                         .Where(IsRemotableNonInfrastructureTessage)
                                         .Concat(handledTeventTypes)
                                         .ToHashSet();

      _typeMap.AssertMappingsExistFor(handledTypes);

      return handledTypes.Select(_typeMap.GetId)
                         .ToHashSet();

      static bool IsRemotableNonInfrastructureTessage(Type tessageType) =>
         tessageType.Implements<IRemotableTessage>() && !tessageType.Implements<TessageTypesInternal.ITessage>();
   }
}