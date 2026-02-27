using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Core.Tessaging.Teventive.Public;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.SystemCE.ThreadingCE;
using Compze.Utilities.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;


public static class TessageHandlerRegistryRegistrar
{
   public static IComponentRegistrar TessageHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, ITessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TessageHandlerRegistry(typeMapper)));
}

//performance: Use static caching + indexing trick for storing and retrieving values throughout this class. TueryTypeIndexFor<TTuery>.Index. Etc
public sealed class TessageHandlerRegistry(ITypeMapper typeMapper) : ITessageHandlerRegistrar, ITessageHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, Action<object>> _tommandHandlers = new Dictionary<Type, Action<object>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent>>> _teventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent>>>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tueryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tommandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyList<TeventHandlerRegistration> _teventHandlerRegistrations = new List<TeventHandlerRegistration>();

   readonly IMonitorCE _monitor = IMonitorCE.WithDefaultTimeout();

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

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTommand>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _tommandHandlersReturningResults, typeof(TTommand), new TommandHandlerWithResultRegistration<TTommand, TResult>(handler));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) => _monitor.Locked(() =>
   {
      TessageInspector.AssertValid<TTuery>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _tueryHandlers, typeof(TTuery), new TueryHandlerRegistration<TTuery, TResult>(handler));
      return this;
   });

   Action<object> ITessageHandlerRegistry.GetTommandHandler(ITommand tessage)
   {
      if(TryGetTommandHandler(tessage, out var handler)) return handler;

      throw new NoHandlerException(tessage.GetType());
   }

   bool TryGetTommandHandler(ITommand tessage, [MaybeNullWhen(false)]out Action<object> handler) =>
      _tommandHandlers.TryGetValue(tessage.GetType(), out handler);

   public Func<ITommand, object> GetTommandHandlerWithReturnValue(Type tommandType) => _tommandHandlersReturningResults[tommandType].HandlerMethod;

   public Action<ITommand> GetTommandHandler(Type tommandType) => _tommandHandlers[tommandType];

   public Func<ITuery<object>, object> GetTueryHandler(Type tommandType) => _tueryHandlers[tommandType].HandlerMethod;

   //performance: Use static caching trick.
   public IReadOnlyList<Action<ITevent>> GetTeventHandlers(Type teventType) => _teventHandlers.Where(it => it.Key.IsAssignableFrom(teventType)).SelectMany(it => it.Value).ToList();

   public Func<IStrictlyLocalTuery<TTuery, TResult>, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      //Urgent: If we don't actually use the TTuery type parameter to do static caching here, remove it.
      if(_tueryHandlers.TryGetValue(tuery.GetType(), out var handler))
      {
         return actualTuery => (TResult)handler.HandlerMethod(actualTuery);
      }

      throw new NoHandlerException(tuery.GetType());
   }

   public Func<ITommand<TResult>, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand)
   {
      if(_tommandHandlersReturningResults.TryGetValue(tommand.GetType(), out var handler))
      {
         return actualTommand => (TResult)handler.HandlerMethod(actualTommand);
      }

      throw new NoHandlerException(tommand.GetType());
   }

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
                                         .Concat(_tommandHandlersReturningResults.Keys)
                                         .Concat(_tueryHandlers.Keys)
                                         .Concat(_teventHandlerRegistrations.Select(reg => reg.Type))
                                         .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                         .Where(tessageType => !tessageType.Implements<TessageTypesInternal.ITessage>())
                                         .ToHashSet();

      var remoteResultTypes = _tommandHandlersReturningResults
                             .Where(handler => handler.Key.Implements<IRemotableTessage>())
                             .Where(handler => handler.Value.ReturnValueType.Implements<IRemotableTessage>())
                             .Select(handler => handler.Value.ReturnValueType)
                             .ToList();

      var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

      _typeMapper.AssertMappingsExistFor(typesNeedingMappings);

      return handledTypes.Select(_typeMapper.GetId)
                         .ToHashSet();
   }

   public class TeventHandlerRegistration(Type type, Action<ITeventHandlerRegistrar<ITevent>> registerHandlerWithRegistrar)
   {
      public Type Type { get; } = type;
      public Action<ITeventHandlerRegistrar<ITevent>> RegisterHandlerWithRegistrar { get; } = registerHandlerWithRegistrar;
   }

   public abstract class HandlerWithResultRegistration(Type returnValueType, Func<object, object> handlerMethod)
   {
      public Type ReturnValueType { get; } = returnValueType;
      public Func<object, object> HandlerMethod { get; } = handlerMethod;
   }

   public class TommandHandlerWithResultRegistration<TTommand, TResult>(Func<TTommand, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                                        tommand => handlerMethod((TTommand)tommand)!);

   public class TueryHandlerRegistration<TTuery, TResult>(Func<TTuery, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                        tommand => handlerMethod((TTuery)tommand)!);
}