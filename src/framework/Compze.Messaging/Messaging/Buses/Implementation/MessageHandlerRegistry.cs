using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Compze.Messaging.Events;
using Compze.Refactoring.Naming;
using Compze.SystemCE.CollectionsCE.GenericCE;
using Compze.SystemCE.LinqCE;
using Compze.SystemCE.ReflectionCE;
using Compze.SystemCE.ThreadingCE;
using Compze.SystemCE.ThreadingCE.ResourceAccess;

namespace Compze.Messaging.Buses.Implementation;

//performance: Use static caching + indexing trick for storing and retrieving values throughout this class. QueryTypeIndexFor<TQuery>.Index. Etc
class MessageHandlerRegistry(ITypeMapper typeMapper) : IMessageHandlerRegistrar, IMessageHandlerRegistry
{
   readonly ITypeMapper _typeMapper = typeMapper;
   IReadOnlyDictionary<Type, Action<object>> _commandHandlers = new Dictionary<Type, Action<object>>();
   IReadOnlyDictionary<Type, IReadOnlyList<Action<IEvent>>> _eventHandlers = new Dictionary<Type, IReadOnlyList<Action<IEvent>>>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _queryHandlers = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyDictionary<Type, HandlerWithResultRegistration> _commandHandlersReturningResults = new Dictionary<Type, HandlerWithResultRegistration>();
   IReadOnlyList<EventHandlerRegistration> _eventHandlerRegistrations = new List<EventHandlerRegistration>();

   readonly MonitorCE _monitor = MonitorCE.WithDefaultTimeout();

   IMessageHandlerRegistrar IMessageHandlerRegistrar.ForEvent<TEvent>(Action<TEvent> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TEvent>();
      _eventHandlers.TryGetValue(typeof(TEvent), out var currentEventSubscribers);
      currentEventSubscribers ??= new List<Action<IEvent>>();

      ThreadSafe.AddToCopyAndReplace(ref _eventHandlers, typeof(TEvent), currentEventSubscribers.AddToCopy(@event => handler((TEvent)@event)));
      ThreadSafe.AddToCopyAndReplace(ref _eventHandlerRegistrations, new EventHandlerRegistration(typeof(TEvent), registrar => registrar.For(handler)));
      return this;
   });

   IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand>(Action<TCommand> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TCommand>();

      if(typeof(TCommand).Implements(typeof(ICommand<>)))
      {
         throw new Exception($"{typeof(TCommand)} expects a result. You must register a method that returns a result.");
      }

      ThreadSafe.AddToCopyAndReplace(ref _commandHandlers, typeof(TCommand), command => handler((TCommand)command));
      return this;
   });

   IMessageHandlerRegistrar IMessageHandlerRegistrar.ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TCommand>();

      ThreadSafe.AddToCopyAndReplace(ref _commandHandlersReturningResults, typeof(TCommand), new CommandHandlerWithResultRegistration<TCommand, TResult>(handler));
      return this;
   });

   IMessageHandlerRegistrar IMessageHandlerRegistrar.ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) => _monitor.Update(() =>
   {
      MessageInspector.AssertValid<TQuery>();

      ThreadSafe.AddToCopyAndReplace(ref _queryHandlers, typeof(TQuery), new QueryHandlerRegistration<TQuery, TResult>(handler));
      return this;
   });

   Action<object> IMessageHandlerRegistry.GetCommandHandler(ICommand message)
   {
      if(TryGetCommandHandler(message, out var handler)) return handler;

      throw new NoHandlerException(message.GetType());
   }

   bool TryGetCommandHandler(ICommand message, [MaybeNullWhen(false)]out Action<object> handler) =>
      _commandHandlers.TryGetValue(message.GetType(), out handler);

   public Func<ICommand, object> GetCommandHandlerWithReturnValue(Type commandType) => _commandHandlersReturningResults[commandType].HandlerMethod;

   public Action<ICommand> GetCommandHandler(Type commandType) => _commandHandlers[commandType];

   public Func<IQuery<object>, object> GetQueryHandler(Type queryType) => _queryHandlers[queryType].HandlerMethod;

   //performance: Use static caching trick.
   public IReadOnlyList<Action<IEvent>> GetEventHandlers(Type eventType) => _eventHandlers.Where(it => it.Key.IsAssignableFrom(eventType)).SelectMany(it => it.Value).ToList();

   public Func<IStrictlyLocalQuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : IStrictlyLocalQuery<TQuery, TResult>
   {
      //Urgent: If we don't actually use the TQuery type parameter to do static caching here, remove it.
      if(_queryHandlers.TryGetValue(query.GetType(), out var handler))
      {
         return actualQuery => (TResult)handler.HandlerMethod(actualQuery);
      }

      throw new NoHandlerException(query.GetType());
   }

   public Func<ICommand<TResult>, TResult> GetCommandHandler<TResult>(ICommand<TResult> command)
   {
      if(_commandHandlersReturningResults.TryGetValue(command.GetType(), out var handler))
      {
         return actualQuery => (TResult)handler.HandlerMethod(actualQuery);
      }

      throw new NoHandlerException(command.GetType());
   }

   IEventDispatcher<IEvent> IMessageHandlerRegistry.CreateEventDispatcher()
   {
      var dispatcher = new CallMatchingHandlersInRegistrationOrderEventDispatcher<IEvent>();
      var registrar = dispatcher.Register()
                                .IgnoreUnhandled<IEvent>();

      _eventHandlerRegistrations.ForEach(handlerRegistration => handlerRegistration.RegisterHandlerWithRegistrar(registrar));

      return dispatcher;
   }

   public ISet<TypeId> HandledRemoteMessageTypeIds()
   {
      var handledTypes = _commandHandlers.Keys
                                         .Concat(_commandHandlersReturningResults.Keys)
                                         .Concat(_queryHandlers.Keys)
                                         .Concat(_eventHandlerRegistrations.Select(reg => reg.Type))
                                         .Where(messageType => messageType.Implements<IRemotableMessage>())
                                         .Where(messageType => !messageType.Implements<MessageTypes.Internal.IMessage>())
                                         .ToHashSet();

      var remoteResultTypes = _commandHandlersReturningResults
                             .Where(handler => handler.Key.Implements<IRemotableMessage>())
                             .Select(handler => handler.Value.ReturnValueType)
                             .ToList();

      var typesNeedingMappings = handledTypes.Concat(remoteResultTypes);

      _typeMapper.AssertMappingsExistFor(typesNeedingMappings);

      return handledTypes.Select(_typeMapper.GetId)
                         .ToHashSet();
   }

   class EventHandlerRegistration(Type type, Action<IEventHandlerRegistrar<IEvent>> registerHandlerWithRegistrar)
   {
      public Type Type { get; } = type;
      public Action<IEventHandlerRegistrar<IEvent>> RegisterHandlerWithRegistrar { get; } = registerHandlerWithRegistrar;
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