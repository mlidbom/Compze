using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.Abstractions.Tessaging.Validation;
using Compze.DependencyInjection.Abstractions;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Typermedia.HandlerRegistration;

public sealed class TypermediaHandlerRegistry(ITypeMap typeMap) : ITypermediaHandlerRegistrar, ITypermediaHandlerRegistry
{
   readonly ITypeMap _typeMap = typeMap;
   IReadOnlyDictionary<Type, TueryHandlerRegistration> _tueryHandlers = new Dictionary<Type, TueryHandlerRegistration>();
   IReadOnlyDictionary<Type, TommandHandlerWithResultRegistration> _tommandHandlersReturningResults = new Dictionary<Type, TommandHandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, Action<object, IUnitOfWorkResolver>> _voidTommandHandlers = new Dictionary<Type, Action<object, IUnitOfWorkResolver>>();

   readonly IMonitor _monitor = IMonitor.New();

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) => _monitor.Locked(() =>
   {
      TessageTypeInspector.AssertValid(typeof(TTommand));

      Interlocked.Exchange(ref _voidTommandHandlers, _voidTommandHandlers.AddToCopy(typeof(TTommand), (tommand, unitOfWork) => handler((TTommand)tommand, unitOfWork)));
      return this;
   });

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand, TResult>(Func<TTommand, IUnitOfWorkResolver, TResult> handler) => _monitor.Locked(() =>
   {
      TessageTypeInspector.AssertValid(typeof(TTommand));

      Interlocked.Exchange(ref _tommandHandlersReturningResults, _tommandHandlersReturningResults.AddToCopy(typeof(TTommand), new TommandHandlerWithResultRegistration(typeof(TResult), (tommand, unitOfWork) => handler((TTommand)tommand, unitOfWork)!)));
      return this;
   });

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, IScopeResolver, TResult> handler) => _monitor.Locked(() =>
   {
      TessageTypeInspector.AssertValid(typeof(TTuery));

      Interlocked.Exchange(ref _tueryHandlers, _tueryHandlers.AddToCopy(typeof(TTuery), new TueryHandlerRegistration((tuery, scope) => handler((TTuery)tuery, scope)!)));
      return this;
   });

   public Action<ITommand, IUnitOfWorkResolver> GetVoidTommandHandler(ITommand tommand)
   {
      if(_voidTommandHandlers.TryGetValue(tommand.GetType(), out var handler))
      {
         return (actualTommand, unitOfWork) => handler(actualTommand, unitOfWork);
      }

      throw new NoHandlerException(tommand.GetType());
   }

   public Func<ITommand, IUnitOfWorkResolver, object> GetTommandHandlerWithReturnValue(Type tommandType) => _tommandHandlersReturningResults[tommandType].HandlerMethod;

   public Func<ITuery<object>, IScopeResolver, object> GetTueryHandler(Type tueryType) => _tueryHandlers[tueryType].HandlerMethod;

   public Func<IStrictlyLocalTuery<TTuery, TResult>, IScopeResolver, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      if(_tueryHandlers.TryGetValue(tuery.GetType(), out var handler))
      {
         return (actualTuery, scope) => (TResult)handler.HandlerMethod(actualTuery, scope);
      }

      throw new NoHandlerException(tuery.GetType());
   }

   public Func<ITommand<TResult>, IUnitOfWorkResolver, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand)
   {
      if(_tommandHandlersReturningResults.TryGetValue(tommand.GetType(), out var handler))
      {
         return (actualTommand, unitOfWork) => (TResult)handler.HandlerMethod(actualTommand, unitOfWork);
      }

      throw new NoHandlerException(tommand.GetType());
   }

   public ISet<TypeId> HandledRemoteTypermediaTypeIds()
   {
      var handledTypes = _tommandHandlersReturningResults.Keys
                                                         .Concat(_tueryHandlers.Keys)
                                                         .Concat(_voidTommandHandlers.Keys)
                                                         .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                                         .Where(tessageType => !tessageType.Implements<IInternalInfrastructureTessage>())
                                                         .ToHashSet();

      var remoteResultTypes = _tommandHandlersReturningResults
                             .Where(handler => handler.Key.Implements<IRemotableTessage>())
                             .Where(handler => handler.Value.ReturnValueType.Implements<IRemotableTessage>())
                             .Select(handler => handler.Value.ReturnValueType)
                             .ToList();

      var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

      _typeMap.AssertMappingsExistFor(typesNeedingMappings);

      return handledTypes.Select(_typeMap.GetId)
                         .ToHashSet();
   }

   //Separately-typed registrations, deliberately: a tommand handler runs in the unit of work its execution is, while a tuery handler runs in a plain scope - the resolver types state which.
   class TommandHandlerWithResultRegistration(Type returnValueType, Func<object, IUnitOfWorkResolver, object> handlerMethod)
   {
      internal Type ReturnValueType { get; } = returnValueType;
      internal Func<object, IUnitOfWorkResolver, object> HandlerMethod { get; } = handlerMethod;
   }

   class TueryHandlerRegistration(Func<object, IScopeResolver, object> handlerMethod)
   {
      internal Func<object, IScopeResolver, object> HandlerMethod { get; } = handlerMethod;
   }
}
