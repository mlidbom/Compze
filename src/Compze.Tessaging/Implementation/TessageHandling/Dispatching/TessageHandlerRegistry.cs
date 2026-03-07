using System.Diagnostics.CodeAnalysis;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Internals.SystemCE.Core.CollectionsCE.GenericCE;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;


public static class TessageHandlerRegistryRegistrar
{
   public static IComponentRegistrar TessageHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, ITessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TessageHandlerRegistry(typeMapper)));
}

sealed class TessageHandlerRegistry(ITypeMapper typeMapper) : ITessageHandlerRegistrar, ITessageHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, Action<object>> _tommandHandlers = new Dictionary<Type, Action<object>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent>>> _teventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent>>>();
   IReadOnlyList<TeventHandlerRegistration> _teventHandlerRegistrations = new List<TeventHandlerRegistration>();

   readonly IMonitor _monitor = IMonitor.New();

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTevent<TTevent>(Action<TTevent> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTevent>();
      _teventHandlers.TryGetValue(typeof(TTevent), out var currentTeventSubscribers);
      currentTeventSubscribers ??= new List<Action<ITevent>>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _teventHandlers, typeof(TTevent), ReadonlyCollectionsCE.AddToCopy(currentTeventSubscribers, tevent => handler((TTevent)tevent)));
      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _teventHandlerRegistrations, new TeventHandlerRegistration(typeof(TTevent), registrar => registrar.For(handler)));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTommand<TTommand>(Action<TTommand> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTommand>();

      if(typeof(TTommand).Implements(typeof(ITommand<>)))
      {
         throw new Exception($"{typeof(TTommand)} expects a result. You must register a method that returns a result.");
      }

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _tommandHandlers, typeof(TTommand), tommand => handler((TTommand)tommand));
      return this;
   });

   Action<object> ITessageHandlerRegistry.GetTommandHandler(ITommand tessage)
   {
      if(TryGetTommandHandler(tessage, out var handler)) return handler;

      throw new NoHandlerException(tessage.GetType());
   }

   bool TryGetTommandHandler(ITommand tessage, [NotNullWhen(true)]out Action<object>? handler) =>
      _tommandHandlers.TryGetValue(tessage.GetType(), out handler);

   public Action<ITommand> GetTommandHandler(Type tommandType) => _tommandHandlers[tommandType];

   //performance: Use static caching trick.
   public IReadOnlyList<Action<ITevent>> GetTeventHandlers(Type teventType) => _teventHandlers.Where(it => it.Key.IsAssignableFrom(teventType)).SelectMany(it => it.Value).ToList();

   ITeventDispatcher<ITevent> ITessageHandlerRegistry.CreateTeventDispatcher()
   {
      var dispatcher = IMutableTeventDispatcher<ITevent>.New();
      var registrar = dispatcher.Register()
                                .IgnoreUnhandled<ITevent>();

      _teventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));

      return dispatcher;
   }

   public ISet<TypeId> HandledRemoteTessageTypeIds()
   {
      var handledTypes = _tommandHandlers.Keys
                                         .Concat(_teventHandlerRegistrations.Select(reg => reg.Type))
                                         .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                         .Where(tessageType => !tessageType.Implements<TessageTypesInternal.ITessage>())
                                         .ToHashSet();

      _typeMapper.AssertMappingsExistFor(handledTypes);

      return handledTypes.Select(_typeMapper.GetId)
                         .ToHashSet();
   }

   class TeventHandlerRegistration(Type type, Action<ITeventHandlerRegistrar<ITevent>> registerHandlerWithRegistrar)
   {
      internal Type Type { get; } = type;
      internal Action<ITeventHandlerRegistrar<ITevent>> RegisterHandlerWithRegistrar { get; } = registerHandlerWithRegistrar;
   }
}