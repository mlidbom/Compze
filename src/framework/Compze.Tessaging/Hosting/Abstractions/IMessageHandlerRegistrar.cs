using System;
using Compze.Tessaging.Abstractions;

namespace Compze.Tessaging.Hosting.Abstractions;

public interface IMessageHandlerRegistrar
{
    IMessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : IEvent;
    IMessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ICommand;
    IMessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : ICommand<TResult>;
    IMessageHandlerRegistrar ForQuery<TQuery, TResult>(Func<TQuery, TResult> handler) where TQuery : IQuery<TResult>;
}