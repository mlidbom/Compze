using System;
using System.Collections.Generic;
using Compze.Abstractions.Internal;
using Compze.Tessaging.Abstractions;
using Compze.Tessaging.Teventive.Abstractions;

namespace Compze.Tessaging.Hosting.Implementation.Abstractions;

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