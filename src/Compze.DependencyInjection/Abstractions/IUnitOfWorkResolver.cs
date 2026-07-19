namespace Compze.DependencyInjection.Abstractions;

///<summary>
/// The resolver of a unit of work: an <see cref="IScopeResolver"/> whose <see cref="IScope"/> is paired with an ambient<br/>
/// transaction — one scope and one transaction, begun and completed together, so everything executed through it either<br/>
/// commits as a whole or rolls back as a whole.
///</summary>
///<remarks>
/// Most framework code does not accept any old scope — it requires running in a unit of work. This interface states that<br/>
/// requirement in the signature: code handed an <see cref="IUnitOfWorkResolver"/> knows it runs inside one, while code handed<br/>
/// a plain <see cref="IScopeResolver"/> knows only that a scope exists. The two are different execution contexts: a tuery<br/>
/// execution, for example, is deliberately a scope with no transaction — it changes nothing, so it is not a unit of work.
///</remarks>
///<remarks>
/// Never registered in, and never resolvable from, the container: whether a scope is a unit of work is decided by the code<br/>
/// that begins the scope, not by the container, so only that code can grant this typing — through<br/>
/// <c>ExecuteUnitOfWork</c>, or through <c>UnitOfWorkResolver.From</c> where an ambient transaction is asserted to exist.
///</remarks>
public interface IUnitOfWorkResolver : IScopeResolver
{
   ///<summary>The identity of this unit of work: value-equal exactly when it identifies the same unit of work, for keying<br/>
   /// logs, caches, and idempotence checks on "which unit of work did this".</summary>
   UnitOfWorkId Id { get; }

   ///<summary>Registers <paramref name="action"/> to run once, after this unit of work has committed successfully — the<br/>
   /// send-on-commit pattern: work that must happen only if the unit of work's effects became real. Never runs when the unit<br/>
   /// of work rolls back. Runs after commit, so it cannot join the committed transaction — anything it does is its own,<br/>
   /// separate work.</summary>
   void OnCommittedSuccessfully(Action action);

   ///<summary>Registers <paramref name="action"/> to run when this unit of work completes, however it ends — committed or<br/>
   /// rolled back: the cleanup pattern, for releasing whatever was held for the unit of work's duration.</summary>
   void OnCompleted(Action action);
}
