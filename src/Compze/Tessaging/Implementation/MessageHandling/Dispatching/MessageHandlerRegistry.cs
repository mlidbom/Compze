using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Hosting.MessageHandling.Registration.Public;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Infrastructure.Validation;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time;
using Compze.Tessaging.Implementation.Abstractions;
using Compze.Tessaging.Implementation.MessageHandling.Abstractions;
using Compze.Utilities.DependencyInjection;
using Compze.Utilities.DependencyInjection.Abstractions;
using Compze.Utilities.SystemCE.CollectionsCE.GenericCE;
using Compze.Utilities.SystemCE.LinqCE;
using Compze.Utilities.SystemCE.ReflectionCE;
using Compze.Utilities.Threading;
using Compze.Utilities.Threading.ResourceAccess;

namespace Compze.Tessaging.Implementation.MessageHandling.Dispatching;


static class MessageHandlerRegistryRegistrar
{
   internal static IComponentRegistrar MessageHandlerRegistry(this IComponentRegistrar registrar)
      => registrar.Register(Singleton.For<ITessageHandlerRegistrar, IMessageHandlerRegistry, TessageHandlerRegistry>()
                                     .CreatedBy((ITypeMapper typeMapper) => new TessageHandlerRegistry(typeMapper)));
}

//performance: Use static caching + indexing trick for storing and retrieving values throughout this class. QueryTypeIndexFor<TQuery>.Index. Etc
class TessageHandlerRegistry(ITypeMapper typeMapper) : ITessageHandlerRegistrar, IMessageHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<ITevent>>> _eventHandlers = new Dictionary<Type, IReadOnlyList<Action<ITevent>>>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _queryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _commandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyList<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TEvent>();
      _eventHandlers.TryGetValue(typeof(TEvent), out var currentEventSubscribers);
      currentEventSubscribers ??= new List<Action<ITevent>>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _eventHandlers, typeof(TEvent), ReadonlyCollectionsCE.AddToCopy(currentEventSubscribers, @event => handler((TEvent)@event)));
      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _eventHandlerRegistrations, new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TCommand>();

      if(typeof(TCommand).Implements(typeof(ITommand<>)))
      {
         throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
      }

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _commandHandlers, typeof(TCommand), command => handler((TCommand)command));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TCommand>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _commandHandlersReturningResults, typeof(TCommand), new CommandHandlerWithResultRegistration<TCommand, TResult>(handler));
      return this;
   });

   ITessageHandlerRegistrar ITessageHandlerRegistrar.ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TQuery>();

      OnlyWithinLocksThreadingHelpers.AddToCopyAndReplace(ref _queryHandlers, typeof(TQuery), new QueryHandlerRegistration<TQuery, TResult>(handler));
      return this;
   });

   Action<object> IMessageHandlerRegistry.GetCommandHandler(ITommand message)
   {
      if(TryGetCommandHandler(message, out var handler)) return handler;

      throw new NoHandlerException(message.GetType());
   }

   bool TryGetCommandHandler(ITommand message, [MaybeNullWhen(false)]out Action<object> handler) =>
      _commandHandlers.TryGetValue(message.GetType(), out handler);

   public Func<ITommand, object> GetCommandHandlerWithReturnValue(Type commandType) => _commandHandlersReturningResults[commandType].HandlerMethod;

   public Action<ITommand> GetCommandHandler(Type commandType) => _commandHandlers[commandType];

   public Func<ITuery<object>, object> GetQueryHandler(Type queryType) => _queryHandlers[queryType].HandlerMethod;

   //performance: Use static caching trick.
   public IReadOnlyList<Action<ITevent>> GetEventHandlers(Type eventType) => _eventHandlers.Where(it => it.Key.IsAssignableFrom(eventType)).SelectMany(it => it.Value).ToList();

   public Func<IStrictlyLocalTuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(IStrictlyLocalTuery<TQuery, TResult> tuery) where TQuery : IStrictlyLocalTuery<TQuery, TResult>
   {
      //Urgent: If we don't actually use the TQuery type parameter to do static caching here, remove it.
      if(_queryHandlers.TryGetValue(tuery.GetType(), out var handler))
      {
         return actualQuery => (TResult)handler.HandlerMethod(actualQuery);
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

   IEventDispatcher<ITevent> IMessageHandlerRegistry.CreateEventDispatcher()
   {
      var dispatcher = IMutableEventDispatcher<ITevent>.New();
      var registrar = dispatcher.Register()
                                .IgnoreUnhandled<ITevent>();

      _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));

      return dispatcher;
   }

   public ISet<TypeId> HandledRemoteMessageTypeIds()
   {
      var handledTypes = _commandHandlers.Keys
                                         .Concat(_commandHandlersReturningResults.Keys)
                                         .Concat(_queryHandlers.Keys)
                                         .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                         .Where(messageType => messageType.Implements<IRemotableTessage>())
                                         .Where(messageType => !messageType.Implements<MessageTypesInternal.IMessage>())
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

   class QueryHandlerRegistration<TQuery, TResult>(Func<TQuery, TResult> handlerMethod) : HandlerWithResultRegistration(typeof(TResult),
                                                                                                                        command => handlerMethod((TQuery)command) ?? throw new Exception("You cannot return null from a query handler"));
}