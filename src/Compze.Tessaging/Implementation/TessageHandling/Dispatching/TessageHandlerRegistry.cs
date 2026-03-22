using System.Diagnostics.CodeAnalysis;
using Compze.TypeIdentifiers;
using Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;


public static class TessageHandlerRegistryRegistrar
{
   public static IComponentRegistrar TessageHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, ITessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((IStructuralTypeMapper typeMapper) => new TessageHandlerRegistry(typeMapper)));
}

sealed class TessageHandlerRegistry(IStructuralTypeMapper typeMapper) : ITessageHandlerRegistrar, ITessageHandlerRegistry
{
   readonly IStructuralTypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, Action<object, IScopeResolver>> _tommandHandlers = new Dictionary<Type, Action<object, IScopeResolver>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>> _teventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent, IScopeResolver>>>();
   IReadOnlyList<Type> _registeredTeventTypes = new List<Type>();

   readonly IMonitor _monitor = IMonitor.New();

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTevent>();
      _teventHandlers.TryGetValue(typeof(TTevent), out var currentTeventSubscribers);
      currentTeventSubscribers ??= new List<Action<ITevent, IScopeResolver>>();

      IReadOnlyList<Action<ITevent, IScopeResolver>> value = [..currentTeventSubscribers, (tevent, kernel) => handler((TTevent)tevent, kernel)];
      Interlocked.Exchange(ref _teventHandlers, _teventHandlers.AddToCopy(typeof(TTevent), value));
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
   public IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type teventType) => _teventHandlers.Where(it => it.Key.IsAssignableFrom(teventType)).SelectMany(it => it.Value).ToList();

   void ITessageHandlerRegistry.DispatchTevent(ITevent tevent, IScopeResolver scopeResolver)
   {
      var handlers = GetTeventHandlers(tevent.GetType());
      foreach(var handler in handlers)
      {
         handler(tevent, scopeResolver);
      }
   }

   public ISet<StructuralTypeId> HandledRemoteTessageTypeIds()
   {
      var handledTypes = _tommandHandlers.Keys
                                         .Concat(_registeredTeventTypes)
                                         .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                         .Where(tessageType => !tessageType.Implements<TessageTypesInternal.ITessage>())
                                         .ToHashSet();

      _typeMapper.AssertMappingsExistFor(handledTypes);

      return handledTypes.Select(_typeMapper.GetId)
                         .ToHashSet();
   }
}