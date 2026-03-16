using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.Abstractions.Tessaging.Validation;
using Compze.Internals.SystemCE.CollectionsCE.GenericCE;

namespace Compze.Typermedia.HandlerRegistration;

public sealed class TypermediaHandlerRegistry(ITypeMapper typeMapper) : ITypermediaHandlerRegistrar, ITypermediaHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tueryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tommandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, Action<object, IServiceLocatorKernel>> _voidTommandHandlers = new Dictionary<Type, Action<object, IServiceLocatorKernel>>();

   readonly IMonitor _monitor = IMonitor.New();

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand>(Action<TTommand, IServiceLocatorKernel> handler) => _monitor.Locked(() =>
   {
      TessageTypeInspector.AssertValid(typeof(TTommand));

      Interlocked.Exchange(ref _voidTommandHandlers, _voidTommandHandlers.AddToCopy(typeof(TTommand), (tommand, kernel) => handler((TTommand)tommand, kernel)));
      return this;
   });

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand, TResult>(Func<TTommand, IServiceLocatorKernel, TResult> handler) => _monitor.Locked(() =>
   {
      TessageTypeInspector.AssertValid(typeof(TTommand));

      Interlocked.Exchange(ref _tommandHandlersReturningResults, _tommandHandlersReturningResults.AddToCopy(typeof(TTommand), new TommandHandlerWithResultRegistration<TTommand, TResult>(handler)));
      return this;
   });

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, IServiceLocatorKernel, TResult> handler) => _monitor.Locked(() =>
   {
      TessageTypeInspector.AssertValid(typeof(TTuery));

      Interlocked.Exchange(ref _tueryHandlers, _tueryHandlers.AddToCopy(typeof(TTuery), new TueryHandlerRegistration<TTuery, TResult>(handler)));
      return this;
   });

   public Action<ITommand, IServiceLocatorKernel> GetVoidTommandHandler(ITommand tommand)
   {
      if(_voidTommandHandlers.TryGetValue(tommand.GetType(), out var handler))
      {
         return (actualTommand, kernel) => handler(actualTommand, kernel);
      }

      throw new NoHandlerException(tommand.GetType());
   }

   public Func<ITommand, IServiceLocatorKernel, object> GetTommandHandlerWithReturnValue(Type tommandType) => _tommandHandlersReturningResults[tommandType].HandlerMethod;

   public Func<ITuery<object>, IServiceLocatorKernel, object> GetTueryHandler(Type tueryType) => _tueryHandlers[tueryType].HandlerMethod;

   public Func<IStrictlyLocalTuery<TTuery, TResult>, IServiceLocatorKernel, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      if(_tueryHandlers.TryGetValue(tuery.GetType(), out var handler))
      {
         return (actualTuery, kernel) => (TResult)handler.HandlerMethod(actualTuery, kernel);
      }

      throw new NoHandlerException(tuery.GetType());
   }

   public Func<ITommand<TResult>, IServiceLocatorKernel, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand)
   {
      if(_tommandHandlersReturningResults.TryGetValue(tommand.GetType(), out var handler))
      {
         return (actualTommand, kernel) => (TResult)handler.HandlerMethod(actualTommand, kernel);
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

      _typeMapper.AssertMappingsExistFor(typesNeedingMappings);

      return handledTypes.Select(_typeMapper.GetId)
                         .ToHashSet();
   }

   abstract class HandlerWithResultRegistration(Type returnValueType, Func<object, IServiceLocatorKernel, object> handlerMethod)
   {
      internal Type ReturnValueType { get; } = returnValueType;
      internal Func<object, IServiceLocatorKernel, object> HandlerMethod { get; } = handlerMethod;
   }

   class TommandHandlerWithResultRegistration<TTommand, TResult>(Func<TTommand, IServiceLocatorKernel, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                                                               (tommand, kernel) => handlerMethod((TTommand)tommand, kernel)!);

   class TueryHandlerRegistration<TTuery, TResult>(Func<TTuery, IServiceLocatorKernel, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                                               (tuery, kernel) => handlerMethod((TTuery)tuery, kernel)!);
}
