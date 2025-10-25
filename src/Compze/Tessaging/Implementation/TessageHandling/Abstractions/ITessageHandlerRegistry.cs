using System;
using System.Collections.Generic;
using Compze.Core.Refactoring.Naming.Internal;
using Compze.Core.Tessaging.Public;
using Compze.Core.Tessaging.Teventive.Public;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

interface ITessageHandlerRegistry
{
    Action<object> GetTommandHandler(ITommand tessage);

    Action<ITommand> GetTommandHandler(Type tommandType);
    Func<ITommand, object> GetTommandHandlerWithReturnValue(Type tommandType);
    Func<ITuery<object>, object> GetTueryHandler(Type tommandType);
    IReadOnlyList<Action<ITevent>> GetTeventHandlers(Type teventType);

    Func<IStrictlyLocalTuery<TTuery, TResult>, TResult> GetTueryHandler<TTuery, TResult>(IStrictlyLocalTuery<TTuery, TResult> tuery) where TTuery : IStrictlyLocalTuery<TTuery, TResult>;

    Func<ITommand<TResult>, TResult> GetTommandHandler<TResult>(ITommand<TResult> tommand);

    ITeventDispatcher<ITevent> CreateTeventDispatcher();

    ISet<TypeId> HandledRemoteTessageTypeIds();
}