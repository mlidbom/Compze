using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ///<summary>Registers a handler for tevents compatible with <typeparamref name="TTevent"/>. The handler receives an<br/>
    /// <see cref="IScopeResolver"/>, not an <see cref="IUnitOfWorkResolver"/>, because tevent delivery spans both context kinds:<br/>
    /// the transactional pipelines run tevent handlers inside a unit of work, but a tevent published through the<br/>
    /// transaction-ignoring escape hatch reaches the same handlers with no transaction, deliberately.</summary>
    ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler) where TTevent : ITevent;

    ///<summary>Registers the handler for <typeparamref name="TTommand"/>. The handler receives the<br/>
    /// <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS: every path that executes a tommand handler runs<br/>
    /// it inside one — a tommand mutates state, so its effects commit or roll back as a whole.</summary>
    ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) where TTommand : ITommand;
}
