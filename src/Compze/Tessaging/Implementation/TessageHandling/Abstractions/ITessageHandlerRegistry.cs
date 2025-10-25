using System;
using System.Collections.Generic;
using Compze.Abstractions.Refactoring.Naming.Internal;
using Compze.Abstractions.Tessaging.Public;
using Compze.Abstractions.Tessaging.Teventive.Public;
using Compze.Abstractions.Time;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface ITessageHandlerRegistry
{
    Action<object> GetCommandHandler(ITommand tessage);

    Action<ITommand> GetCommandHandler(Type commandType);
    Func<ITommand, object> GetCommandHandlerWithReturnValue(Type commandType);
    Func<ITuery<object>, object> GetTueryHandler(Type commandType);
    IReadOnlyList<Action<ITevent>> GetEventHandlers(Type eventType);

    Func<IStrictlyLocalTuery<TTuery, TResult>, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

    Func<ITommand<TResult>, TResult> GetCommandHandler<TResult>(ITommand<TResult> tommand);

    IEventDispatcher<ITevent> CreateEventDispatcher();

    ISet<TypeId> HandledRemoteTessageTypeIds();
}