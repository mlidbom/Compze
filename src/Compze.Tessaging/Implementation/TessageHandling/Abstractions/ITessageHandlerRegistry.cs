using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object, IScopeResolver> GetTommandHandler(ITommand tessage);

    Action<ITommand, IScopeResolver> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type wrapperTeventType);

    ///<summary>The transaction-ignoring tevent handlers — observation, the escape-hatch subscription kind — whose subscriptions match<br/>
    /// <paramref name="wrapperTeventType"/>. Dispatched by <c>TeventObservationDispatcher</c>, never by the transactional pipelines.</summary>
    IReadOnlyList<Action<ITevent, IScopeResolver>> GetTransactionIgnoringTeventHandlers(Type wrapperTeventType);

    ISet<TypeId> HandledRemoteTessageTypeIds();
}