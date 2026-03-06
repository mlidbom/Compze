using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Internals.SystemCE.ReflectionCE;
using Compze.Threading;
using Compze.Threading.ResourceAccess;

namespace Compze.Typermedia.HandlerRegistration;

public class TypermediaHandlerRegistry(ITypeMapper typeMapper, Action<Type> typeValidator, Func<Type, bool> isInternalTessageType) : ITypermediaHandlerRegistrar, ITypermediaHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   readonly Action<Type> _typeValidator = typeValidator;
   readonly Func<Type, bool> _isInternalTessageType = isInternalTessageType;
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tueryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tommandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();

   readonly IMonitor _monitor = IMonitor.New();

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTommand<TTommand, TResult>(Func<TTommand, TResult> handler) => _monitor.Locked(() =>
   {
      _typeValidator(typeof(TTommand));

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _tommandHandlersReturningResults, typeof(TTommand), new TommandHandlerWithResultRegistration<TTommand, TResult>(handler));
      return this;
   });

   ITypermediaHandlerRegistrar ITypermediaHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) => _monitor.Locked(() =>
   {
      _typeValidator(typeof(TTuery));

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _tueryHandlers, typeof(TTuery), new TueryHandlerRegistration<TTuery, TResult>(handler));
      return this;
   });

   public Func<ITommand, object> GetTommandHandlerWithReturnValue(Type tommandType) => _tommandHandlersReturningResults[tommandType].HandlerMethod;

   public Func<ITuery<object>, object> GetTueryHandler(Type tueryType) => _tueryHandlers[tueryType].HandlerMethod;

   public Func<IStrictlyLocalTuery<TTuery, TResult>, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
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

   public ISet<TypeId> HandledRemoteTypermediaTypeIds()
   {
      var handledTypes = _tommandHandlersReturningResults.Keys
                                                         .Concat(_tueryHandlers.Keys)
                                                         .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                                         .Where(tessageType => !_isInternalTessageType(tessageType))
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

   abstract class HandlerWithResultRegistration(Type returnValueType, Func<object, object> handlerMethod)
   {
      internal Type ReturnValueType { get; } = returnValueType;
      internal Func<object, object> HandlerMethod { get; } = handlerMethod;
   }

   class TommandHandlerWithResultRegistration<TTommand, TResult>(Func<TTommand, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                                        tommand => handlerMethod((TTommand)tommand)!);

   class TueryHandlerRegistration<TTuery, TResult>(Func<TTuery, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                        tuery => handlerMethod((TTuery)tuery)!);
}
