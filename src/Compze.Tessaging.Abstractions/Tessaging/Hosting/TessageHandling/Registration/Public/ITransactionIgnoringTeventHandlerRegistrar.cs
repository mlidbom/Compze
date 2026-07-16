using Compze.Abstractions.Tessaging.Public;
using Compze.DependencyInjection.Abstractions;

namespace Compze.Tessaging.Abstractions.Tessaging.Hosting.TessageHandling.Registration.Public;

///<summary>Registers transaction-ignoring tevent handlers — observation, the one subscription-side opt-down from a tevent type's<br/>
/// declared delivery guarantee (see <c>src/Compze.Tessaging/dev_docs/tevent-delivery-model.md</c>). A handler registered here fires<br/>
/// once, immediately, when a matching tevent is registered: at publish time for a local publish, on arrival for a tevent from<br/>
/// another endpoint — always outside any transaction, in its own scope, undeterred by the fate of the transaction the tevent was<br/>
/// published or is processed in.</summary>
///<remarks>The fine print a subscriber accepts by registering here — this rung is for infrastructure (the in-memory cache, the<br/>
/// monitoring tool), never domain logic: the handler may observe doomed tevents (the observed tevent's publishing or processing<br/>
/// transaction may subsequently roll back); ordering relative to the transactional handlers is unspecified; and a throwing handler<br/>
/// is reported through the background-exception reporter, never retried. A separate registrar from<br/>
/// <see cref="ITessageHandlerRegistrar"/>, deliberately: registering here is a visible opt-out of every delivery guarantee, kept<br/>
/// off the common surface.</remarks>
//todo:review: The "TransactionIgnoring" name predates the unit-of-work vocabulary (UnitOfWork*/Independent* doors, IUnitOfWorkResolver). Reconsider the whole family's naming — this registrar, RegisterTransactionIgnoringTeventHandlers, GetTransactionIgnoringTeventHandlers — e.g. around "observation", the delivery model's own word for this rung.
public interface ITransactionIgnoringTeventHandlerRegistrar
{
   ITransactionIgnoringTeventHandlerRegistrar ForTevent<TTevent>(Action<TTevent, IScopeResolver> handler) where TTevent : ITevent;
}
