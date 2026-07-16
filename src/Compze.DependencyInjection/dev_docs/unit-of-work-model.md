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
- **Tevent participation handlers** receive `IUnitOfWorkResolver` too: every delivery path runs them inside
  a unit of work — the publisher's own for a local publish (asserted: `IUnitOfWorkTeventPublisher` throws
  with no ambient transaction), the inbox processing's own for an exactly-once arrival, the direct
  dispatch's own for a transient arrival. Observation handlers, delivered detached from any transaction by
  contract, have their own registrar and keep `IScopeResolver`.

Beyond resolution, the resolver is the unit of work's handle: `Id` (a `UnitOfWorkId`, backed by the
transaction's `LocalIdentifier` — value-equal exactly when it identifies the same unit of work) and the
completion hooks `OnCommittedSuccessfully(action)` / `OnCompleted(action)`, thin veneers over the
`TransactionCompleted`-based callbacks the framework already trusted internally (the outbox's
send-on-commit, the connection cleanup). All three operate on the transaction **captured when the resolver
was created**, never on `Transaction.Current`, so they bind to the unit of work the resolver certifies even
where the ambient transaction has drifted.

The container can never grant the typing — a scope is not necessarily a unit of work, and `IUnitOfWorkResolver`
is never registered nor resolvable. Only the code that pairs scope with transaction can grant it:
`ExecuteUnitOfWork` for the unit of work it begins, or `UnitOfWorkResolver.From(scopeResolver)`, which
asserts the ambient transaction exists and only then wraps. `From`'s callers are the seams where a
caller-provided scope is proven transactional: the local typermedia navigator (an `IStrictlyLocalTommand` is
`IMustBeSentTransactionally`, asserted before dispatch), the inbox's handler execution task, and the
unit-of-work tevent publisher, which asserts its ambient transaction before routing any delivery leg.

## `Lifestyle.UnitOfWork` — the requirement, enforced at resolution

`Lifestyle.UnitOfWork` (`UnitOfWork.For<TService>()`) is a `Scoped` component that additionally requires an
ambient transaction: resolving it with none present throws. The backend containers never see the
lifestyle — the shared registration layer (`ComponentRegistration.CreateBackendRegistration`) hands them a
`Scoped` registration whose instantiation asserts the ambient transaction first — so no backend adapter
knows units of work exist.

`IUnitOfWorkTeventPublisher` and `IUnitOfWorkTommandSender` are registered with it: their every use demands
a transaction, so a resolution outside one is already a bug, and it now fails at the resolve instead of at
first use. The lifestyle is deliberately single-service (no multi-service `For` overloads), because one
instance serving several service types usually pairs an updating face with a reading face — and that is
exactly why the session-style components keep `Scoped`: `TeventStoreUpdater` serves `ITeventStoreReader`,
and `DocumentDbSession` serves `IDocumentDbReader`/`IDocumentDbBulkReader`, from the same instance, and
reads run in plain scopes. `IUnitOfWorkLocalTypermediaNavigator` also stays `Scoped`: it executes tueries
from read scopes. Giving the updater faces the lifestyle would first require splitting session identity
from the reader faces — a modeling question deliberately left open.

The assert fires at instantiation — the first resolution in each scope — so it proves the component is born
inside a unit of work, not that it never outlives its transaction: the per-component usage guards
(`SingleTransactionUsageGuard`) remain the deep defense. A component whose *construction* must survive read
scopes while only its *use* demands the transaction defers instead: `TeventStoreUpdater` takes
`IServiceResolver<IUnitOfWorkTeventPublisher>` (the publisher registers `WithServiceResolver()`, whose
deferred resolver is `Scoped` — the transaction requirement bites at `Resolve()`, the deferral point) and
resolves at publish time, always inside its transaction.

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
  this way, and `IUnitOfWorkResolver`'s hooks surface the same callbacks on the unit of work's own handle.
- **`VolatileTransactionParticipant` stays.** Two participants genuinely need full
  `IEnlistmentNotification` semantics and no callback API can replace them: the SQLite connection, which *is*
  a resource manager (`OnEnlist` takes the write gate and begins the real DB transaction), and DocumentDb's
  flush-on-prepare, which issues new writes during prepare (`EnlistDuringPrepareRequired`) and votes the
  whole transaction down on failure.
- The transport edge's `ExecutionContext.SuppressFlow` (TransportRequestController) is protective: it
  guarantees a remote-arriving handler never inherits its sender's ambient transaction, even for
  same-process loopback.

## Deferred, deliberately

- **A `UnitOfWork` object beyond the resolver** — probably never. Everything reification was to buy landed
  on `IUnitOfWorkResolver` itself: identity (`Id`) and completion hooks (`OnCommittedSuccessfully` /
  `OnCompleted`), both over the captured transaction. What remains unreified — migrating the per-component
  usage guards into a unit-of-work lifecycle — stays unplanned: the guards work, and `Lifestyle.UnitOfWork`
  already fails a wrong-context resolution at the resolve.
- **A type-level name for the isolated-scope (read) context**, if something ever needs to say it in a
  signature.
