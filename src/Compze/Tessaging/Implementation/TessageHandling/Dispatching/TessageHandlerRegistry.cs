using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.TessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation.TessageHandling.Dispatching;


static class TessageHandlerRegistryRegistrar
{
   internal static IComponentRegistrar TessageHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, ITessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TessageHandlerRegistry(typeMapper)));
}

//performance: Use static caching + indexing trick for storing and retrieving values throughout this class. TueryTypeIndexFor<TTuery>.Index. Etc
class TessageHandlerRegistry(ITypeMapper typeMapper) : ITessageHandlerRegistrar, ITessageHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent>>> _eventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent>>>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _tueryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _commandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyList<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler) => _monitor.Update(() =>
   {
      TessageInspector.AssertValid<TEvent>();
      _eventHandlers.TryGetValue(typeof(TEvent), out var currentEventSubscribers);
      currentEventSubscribers ??= new List<Action<ITevent>>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _eventHandlers, typeof(TEvent), ReadonlyCollectionsCE.AddToCopy(currentEventSubscribers, @event => handler((TEvent)@event)));
      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _eventHandlerRegistrations, new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler) => _monitor.Update(() =>
   {
      TessageInspector.AssertValid<TCommand>();

      if(typeof(TCommand).Implements(typeof(ITommand<>)))
      {
         throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
      }

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _commandHandlers, typeof(TCommand), command => handler((TCommand)command));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) => _monitor.Update(() =>
   {
      TessageInspector.AssertValid<TCommand>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _commandHandlersReturningResults, typeof(TCommand), new CommandHandlerWithResultRegistration<TCommand, TResult>(handler));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) => _monitor.Update(() =>
   {
      TessageInspector.AssertValid<TTuery>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _tueryHandlers, typeof(TTuery), new TueryHandlerRegistration<TTuery, TResult>(handler));
      return this;
   });

   Action<object> ITessageHandlerRegistry.GetCommandHandler(ITommand tessage)
   {
      if(TryGetCommandHandler(tessage, out var handler)) return handler;

      throw new NoHandlerException(tessage.GetType());
   }

   bool TryGetCommandHandler(ITommand tessage, [MaybeNullWhen(false)]out Action<object> handler) =>
      _commandHandlers.TryGetValue(tessage.GetType(), out handler);

   public Func<ITommand, object> GetCommandHandlerWithReturnValue(Type commandType) => _commandHandlersReturningResults[commandType].HandlerMethod;

   public Action<ITommand> GetCommandHandler(Type commandType) => _commandHandlers[commandType];

   public Func<ITuery<object>, object> GetTueryHandler(Type tueryType) => _tueryHandlers[tueryType].HandlerMethod;

   //performance: Use static caching trick.
   public IReadOnlyList<Action<ITevent>> GetEventHandlers(Type eventType) => _eventHandlers.Where(it => it.Key.IsAssignableFrom(eventType)).SelectMany(it => it.Value).ToList();

   public Func<IStrictlyLocalTuery<TTuery, TResult>, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>
   {
      //Urgent: If we don't actually use the TTuery type parameter to do static caching here, remove it.
      if(_tueryHandlers.TryGetValue(tuery.GetType(), out var handler))
      {
         return actualTuery => (TResult)handler.HandlerMethod(actualTuery);
      }

      throw new NoHandlerException(tuery.GetType());
   }

   public Func<ITommand<TResult>, TResult> GetCommandHandler<TResult>(ITommand<TResult> tommand)
   {
      if(_commandHandlersReturningResults.TryGetValue(tommand.GetType(), out var handler))
      {
         return actualCommand => (TResult)handler.HandlerMethod(actualCommand);
      }

      throw new NoHandlerException(tommand.GetType());
   }

   IEventDispatcher<ITevent> ITessageHandlerRegistry.CreateEventDispatcher()
   {
      var dispatcher = IMutableEventDispatcher<ITevent>.New();
      var registrar = dispatcher.Register()
                                .IgnoreUnhandled<ITevent>();

      _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));

      return dispatcher;
   }

   public ISet<TypeId> HandledRemoteTessageTypeIds()
   {
      var handledTypes = _commandHandlers.Keys
                                         .Concat(_commandHandlersReturningResults.Keys)
                                         .Concat(_tueryHandlers.Keys)
                                         .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                         .Where(tessageType => tessageType.Implements<IRemotableTessage>())
                                         .Where(tessageType => !tessageType.Implements<TessageTypesInternal.ITessage>())
                                         .ToHashSet();

      var remoteResultTypes = _commandHandlersReturningResults
                             .Where(handler => handler.Key.Implements<IRemotableTessage>())
                             .Where(handler => handler.Value.ReturnValueType.Implements<IRemotableTessage>())
                             .Select(handler => handler.Value.ReturnValueType)
                             .ToList();

      var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

      _typeMapper.AssertMappingsExistFor(typesNeedingMappings);

      return handledTypes.Select(_typeMapper.GetId)
                         .ToHashSet();
   }

   class EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<ITevent>> registerHandlerWithRegistrar)
   {
      public Type Type { get; } = type;
      public Action<IEventHandlerRegistrar<ITevent>> RegisterHandlerWithRegistrar { get; } = registerHandlerWithRegistrar;
   }

   abstract class HandlerWithResultRegistration(Type returnValueType, Func<object, object> handlerMethod)
   {
      internal Type ReturnValueType { get; } = returnValueType;
      internal Func<object, object> HandlerMethod { get; } = handlerMethod;
   }

   class CommandHandlerWithResultRegistration<TCommand, TResult>(Func<TCommand, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                                        command => handlerMethod((TCommand)command) ?? throw new Exception("You cannot return null from a command handler"));

   class TueryHandlerRegistration<TTuery, TResult>(Func<TTuery, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                        command => handlerMethod((TTuery)command) ?? throw new Exception("You cannot return null from a tuery handler"));
}