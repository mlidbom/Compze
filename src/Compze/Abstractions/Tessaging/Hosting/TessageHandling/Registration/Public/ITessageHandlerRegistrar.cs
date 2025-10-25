using System;
using Compze.Abstractions.Tessaging.Public;

namespace Compze.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ITessageHandlerRegistrar ForEvent<TEvent>(Action<TEvent> handler) where TEvent : ITevent;
    ITessageHandlerRegistrar ForCommand<TCommand>(Action<TCommand> handler) where TCommand : ITommand;
    ITessageHandlerRegistrar ForCommand<TCommand, TResult>(Func<TCommand, TResult> handler) where TCommand : ITommand<TResult>;
    ITessageHandlerRegistrar ForTuery<TTuery, TResult>(Func<TTuery, TResult> handler) where TTuery : ITuery<TResult>;
}
