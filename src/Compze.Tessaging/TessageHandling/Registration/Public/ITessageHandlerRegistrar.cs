using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.TessageHandling.Registration.Public;

public interface ITessageHandlerRegistrar
{
    ///<summary>Registers a handler for tevents compatible with <typeparamref name="TTevent"/>. The handler receives the<br/>
    /// <see cref="IUnitOfWorkResolver"/> of the unit of work delivering the tevent: a local publish delivers inside the<br/>
    /// publisher's unit of work, an exactly-once arrival inside the inbox processing's own, and a best-effort arrival inside the<br/>
    /// direct dispatch's own. A handler registered here never runs outside one — delivery detached from any transaction is<br/>
    /// observation, the separate subscription kind (<see cref="ITransactionIgnoringTeventHandlerRegistrar"/>).</summary>
    ITessageHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IUnitOfWorkResolver> handler) where TTevent : ITevent;

    ///<summary>Registers the handler for <typeparamref name="TTommand"/>. The handler receives the<br/>
    /// <see cref="IUnitOfWorkResolver"/> of the unit of work its execution IS: every path that executes a tommand handler runs<br/>
    /// it inside one — a tommand mutates state, so its effects commit or roll back as a whole.</summary>
    ITessageHandlerRegistrar ForTommand<TTommand>(Action<TTommand, IUnitOfWorkResolver> handler) where TTommand : ITommand;
}
