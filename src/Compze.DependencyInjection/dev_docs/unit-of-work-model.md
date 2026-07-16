# The unit-of-work model

> A **unit of work** is one `IScope` paired with one ambient transaction, begun and completed together, so
> everything executed within it either commits as a whole or rolls back as a whole.

The concept always existed in Compze — as fragments. The scope came from the container, the transaction from
`System.Transactions`, their pairing was a convention living in the bodies of runner extension methods, its
invariants were re-asserted per component by hand-assembled usage guards, and nothing in any signature said
which components required it. This document records the model that named it, as built on branch
`unit-of-work-as-proper-abstraction` (2026-07-16).

## The three execution-context kinds

Every piece of work the framework runs falls into exactly one of three context kinds:

| Context | Mechanics | Who runs in it |
|---|---|---|
| **Unit of work** | fresh scope + transaction (`ExecuteUnitOfWork`) | tommand handlers, exactly-once inbox processing, transient tevent dispatch, every independent door's mutating verb |
| **Isolated scope** | fresh scope, no transaction of its own (`ExecuteInIsolatedScope`); an ambient transaction, if the caller has one, is left as-is so reads join its consistency | tuery handlers, discovery queries |
| **Detached** | fresh scope + ambient transaction actively **suppressed** | observation handlers |

A tuery execution is deliberately **not** a unit of work: it changes nothing, so there is nothing to commit
or roll back. (On SQLite this is also load-bearing: any transaction that touches a SQLite connection takes
the per-database write gate, so transactional reads would serialize against writes.) Detached is not the
same as isolated-scope: a read *wants* to see the caller's uncommitted writes when a transaction is ambient;
an observer must survive the caller's rollback, so it suppresses.

## `IUnitOfWorkResolver` — the requirement, stated in the types

`IUnitOfWorkResolver : IScopeResolver` says "this code runs inside a unit of work" in a signature. Most
framework code does not accept any old scope — it requires a unit of work — and the resolver types now
express which:

- `ExecuteUnitOfWork(Action<IUnitOfWorkResolver>)` vs `ExecuteInIsolatedScope(Action<IScopeResolver>)`.
- **Tommand handlers** receive `IUnitOfWorkResolver`: every path that executes one runs it inside a unit of
  work (`IMustBeHandledTransactionally` rides on the tommand tiers). Misregistration is a compile error.
- **Tuery handlers** receive `IScopeResolver`: their execution is a scope, not a unit of work.
- **Tevent handlers** receive `IScopeResolver`, deliberately: the same `ForTevent` registration serves both
  the transactional pipelines *and* the transaction-ignoring escape hatch, whose documented contract is
  delivery detached from any transaction — a unit-of-work claim would lie.

The container can never grant the typing — a scope is not necessarily a unit of work, and `IUnitOfWorkResolver`
is never registered nor resolvable. Only the code that pairs scope with transaction can grant it:
`ExecuteUnitOfWork` for the unit of work it begins, or `UnitOfWorkResolver.From(scopeResolver)`, which
asserts the ambient transaction exists and only then wraps. `From`'s two callers are the seams where a
caller-provided scope is proven transactional: the local typermedia navigator (an `IStrictlyLocalTommand` is
`IMustBeSentTransactionally`, asserted before dispatch) and the inbox's handler execution task.

## The front-door duality: `UnitOfWork*` / `Independent*`

Application-facing operations come in two flavors, named by the caller's relationship to the unit of work —
the same duality `IRootResolver`/`IScopeResolver` expresses at the container level:

| Within the caller's unit of work (Scoped) | As its own unit of work (Singleton, root-resolvable) |
|---|---|
| `IUnitOfWorkTeventPublisher` | `IIndependentTeventPublisher` |
| `IUnitOfWorkTommandSender` (formerly `IServiceBusSession` — never a session: no conversational state) | `IIndependentTommandSender` |
| `IUnitOfWorkLocalTypermediaNavigator` (formerly `IInProcessTypermediaNavigator`) | `IIndependentLocalTypermediaNavigator` (tommands = own unit of work; tueries = own isolated scope) |
| — | `IRemoteTypermediaNavigator`: independent by *nature* — remote typermedia sends are forbidden inside a transaction, so there is no unit-of-work flavor to pair with, and no qualifier |

The independent doors exist because the unit-of-work flavors are scoped, so code outside any scope cannot
resolve them — it used to hand-build the context from container primitives
(`ExecuteInIsolatedScope(scope => scope.Resolve<ITeventPublisher>().Publish(fact))`) to say one domain verb.
An independent door is an ordinary constructor dependency instead.

**Safety lives in asserts, not names.** Each independent door asserts `Transaction.Current == null`: called
from within an ambient transaction, `TransactionScopeOption.Required` would silently *join* it, making
"independent" a lie — so it explodes and points at the unit-of-work flavor. The mirror-direction asserts
predate this model (`TessageValidator`: `IMustBeSentTransactionally` demands a transaction,
`ICannotBeSentRemotelyFromWithinTransaction` forbids one).

## Reuse the battle-tested wheel: System.Transactions IS the ambient unit-of-work

Because a unit of work is transactional *by definition*, `Transaction.Current` (with
`TransactionScopeAsyncFlowOption.Enabled`, as `TransactionScopeCe` always opens it) already is the ambient
unit-of-work tracker — inside a unit of work ⟺ inside its transaction. No hand-rolled ambient-scope
tracking exists or is planned; a considered AsyncLocal design was dropped in its favor. Likewise:

- **Completion callbacks** are `TransactionCompleted`-based (`TransactionCE.OnCommittedSuccessfully` /
  `OnCompleted`) — the outbox's send-on-commit and the transaction-affinity connection cleanup already work
  this way.
- **`VolatileTransactionParticipant` stays.** Two participants genuinely need full
  `IEnlistmentNotification` semantics and no callback API can replace them: the SQLite connection, which *is*
  a resource manager (`OnEnlist` takes the write gate and begins the real DB transaction), and DocumentDb's
  flush-on-prepare, which issues new writes during prepare (`EnlistDuringPrepareRequired`) and votes the
  whole transaction down on failure.
- The transport edge's `ExecutionContext.SuppressFlow` (TransportRequestController) is protective: it
  guarantees a remote-arriving handler never inherits its sender's ambient transaction, even for
  same-process loopback.

## Deferred, deliberately

- **Reifying a `UnitOfWork` object** (completion-hook surface on `IUnitOfWorkResolver`, migrating the
  per-component usage guards into the unit of work's own lifecycle) — sketch first; the guards work.
- **A type-level name for the isolated-scope (read) context**, if something ever needs to say it in a
  signature.
