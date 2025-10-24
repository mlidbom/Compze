using System;
using System.Collections.Generic;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time;

namespace Compze.Tessaging.Implementation.MessageHandling.Abstractions;

interface IMessageHandlerRegistry
{
    Action<object> GetCommandHandler(ICommand message);

    Action<ICommand> GetCommandHandler(Type commandType);
    Func<ICommand, object> GetCommandHandlerWithReturnValue(Type commandType);
    Func<IQuery<object>, object> GetQueryHandler(Type commandType);
    IReadOnlyList<Action<IEvent>> GetEventHandlers(Type eventType);

    Func<IStrictlyLocalQuery<TQuery, TResult>, TResult> GetQueryHandler<TQuery, TResult>(IStrictlyLocalQuery<TQuery, TResult> query) where TQuery : IStrictlyLocalQuery<TQuery, TResult>;

    Func<ICommand<TResult>, TResult> GetCommandHandler<TResult>(ICommand<TResult> command);

    IEventDispatcher<IEvent> CreateEventDispatcher();

    ISet<TypeId> HandledRemoteMessageTypeIds();
}