using Compze.TypeIdentifiers;
using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Implementation.TessageHandling.Abstractions;

public interface ITessageHandlerRegistry
{
    Action<object, IUnitOfWorkResolver> GetTommandHandler(ITommand tessage);

    Action<ITommand, IUnitOfWorkResolver> GetTommandHandler(Type tommandType);
    IReadOnlyList<Action<ITevent, IScopeResolver>> GetTeventHandlers(Type wrapperTeventType);

    ///<summary>The transaction-ignoring tevent handlers — observation, the escape-hatch subscription kind — whose subscriptions match<br/>
    /// <paramref name="wrapperTeventType"/>. Dispatched by <c>TeventObservationDispatcher</c>, never by the transactional pipelines.</summary>
    IReadOnlyList<Action<ITevent, IScopeResolver>> GetTransactionIgnoringTeventHandlers(Type wrapperTeventType);

    ///<summary>The advertised set: the <see cref="TypeId"/> of every remotable, non-infrastructure tessage type this endpoint<br/>
    /// handles, in the form remote routing matches against — tevent subscriptions as their translated wrapper types, tommands as<br/>
    /// they stand. Asserts the set's soundness: every advertised type must be one the peers' routers can serve.</summary>
    ISet<TypeId> HandledRemoteTessageTypeIds();

    ///<summary>The registered handler tessage types whose type declares the exactly-once delivery contract — the types an endpoint<br/>
    /// may only advertise when its composition wires the exactly-once machinery (the inbox that persists, dedups, and retries).<br/>
    /// Observation subscriptions count too: observing a remote exactly-once tevent still requires receiving it exactly-once.<br/>
    /// See <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>.</summary>
    IReadOnlyList<Type> RegisteredTypesDemandingExactlyOnceDelivery();
}