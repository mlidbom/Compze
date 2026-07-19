using System.Transactions;
using Compze.Contracts;
using Compze.Internals.SystemCE.TransactionsCE;
using static Compze.Contracts.Contract;

namespace Compze.DependencyInjection.Runtime.Resolution;

///<summary>
/// The <see cref="IUnitOfWorkResolver"/>: certifies that a scope's resolver operates inside a unit of work — a scope paired<br/>
/// with an ambient transaction — by refusing to be created when no ambient transaction is present.
///</summary>
///<remarks>
/// The container cannot grant unit-of-work typing (a scope is not necessarily a unit of work), so this type is the one gate<br/>
/// through which an <see cref="IScopeResolver"/> earns it: <see cref="From"/> asserts the ambient transaction exists and only<br/>
/// then wraps the resolver. Created by <c>ExecuteUnitOfWork</c> for the unit of work it begins, and by framework code that has<br/>
/// already asserted a transactional context around a caller-provided scope.
///</remarks>
///<remarks>
/// The transaction is captured at creation, and <see cref="Id"/>, <see cref="OnCommittedSuccessfully"/>, and<br/>
/// <see cref="OnCompleted"/> operate on the captured transaction — never on <see cref="Transaction.Current"/> — so they bind<br/>
/// to the unit of work this resolver certifies even when the ambient transaction has drifted, as in a continuation the<br/>
/// ambient context did not flow into.
///</remarks>
public class UnitOfWorkResolver : IUnitOfWorkResolver
{
   ///<summary>Grants unit-of-work typing to <paramref name="scopeResolver"/>. Asserts that an ambient transaction is present —<br/>
   /// a scope alone is not a unit of work; the pairing with a transaction is what makes it one.</summary>
   public static IUnitOfWorkResolver From(IScopeResolver scopeResolver)
   {
      State.Assert(Transaction.Current != null,
                   () => $"A unit of work is a scope paired with an ambient transaction, and there is no ambient transaction. Run the work through ExecuteUnitOfWork, which begins scope and transaction together — or, if this context deliberately has no transaction, accept an {nameof(IScopeResolver)} instead of claiming a unit of work.");
      return new UnitOfWorkResolver(scopeResolver, Transaction.Current);
   }

   readonly IScopeResolver _scopeResolver;
   readonly Transaction _transaction;

   UnitOfWorkResolver(IScopeResolver scopeResolver, Transaction transaction)
   {
      _scopeResolver = scopeResolver;
      _transaction = transaction;
      Id = new UnitOfWorkId(transaction);
   }

   public UnitOfWorkId Id { get; }
   public void OnCommittedSuccessfully(Action action) => _transaction.OnCommittedSuccessfully(action);
   public void OnCompleted(Action action) => _transaction.OnCompleted(action);

   public object Resolve(Type serviceType) => _scopeResolver.Resolve(serviceType);
   public IEnumerable<object> ResolveSet(Type serviceType) => _scopeResolver.ResolveSet(serviceType);
}
