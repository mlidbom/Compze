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
| **Unit of work** | fresh scope + transaction (`ExecuteUnitOfWork` / `ExecuteUnitOfWorkAsync`) | tommand handlers, exactly-once inbox processing, best-effort tevent dispatch, every independent door's mutating verb |
| **Isolated scope** | fresh scope, no transaction of its own (`ExecuteInIsolatedScope` / `ExecuteInIsolatedScopeAsync`); an ambient transaction, if the caller has one, is left as-is so reads join its consistency | tuery handlers, discovery queries |
| **Detached** | fresh scope on a context born transaction-free: the observation dispatch pump starts with `ExecutionContext` flow suppressed, so there is no ambient transaction to suppress — and the pump asserts that guarantee | observation handlers |

A tuery execution is deliberately **not** a unit of work: it changes nothing, so there is nothing to commit
or roll back. (On SQLite this is also load-bearing: any transaction that touches a SQLite connection takes
the per-database write gate, so transactional reads would serialize against writes.) Detached is not the
same as isolated-scope: a read *wants* to see the caller's uncommitted writes when a transaction is ambient;
an observer observes committed facts from outside every transaction, so its context carries none — by
construction since observation dispatch moved off-thread (it queues at the publisher's commit and runs on
the engine's observation dispatch pump).

**The choreography is async-flow-safe.** The async envelope forms open their transaction scope with async
flow enabled, so the ambient transaction flows across the execution's awaits: one unit of work legitimately
migrates across pool threads, and its identity is its transaction, never a thread. Session affinity follows:
the scoped sessions (the document db session, the tevent store updater) guard against serving **two
transactions** — one session belongs to one unit of work — while the thread-affinity guard of the
synchronous era is gone, since thread identity stopped being an invariant of correct executions.

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
  dispatch's own for a best-effort arrival. Observation handlers, delivered detached from any transaction by
  contract, have their own registrar and keep `IScopeResolver`.

Beyond resolution, the resolver is the unit of work's handle: `Id` (a `UnitOfWorkId`, backed by the
transaction's `LocalIdentifier` — value-equal exactly when it identifies the same unit of work) and the
completion hooks `OnCommittedSuccessfully(action)` / `OnCompleted(action)`, thin veneers over the
`TransactionCompleted`-based callbacks the framework already trusted internally (the outbox's
send-on-commit, the connection cleanup, observation's queue-at-commit). All three operate on the transaction **captured when the resolver
was created**, never on `Transaction.Current`, so they bind to the unit of work the resolver certifies even
where the ambient transaction has drifted.

The container can never grant the typing — a scope is not necessarily a unit of work, and `IUnitOfWorkResolver`
is never registered nor resolvable. Only the code that pairs scope with transaction can grant it:
`ExecuteUnitOfWork` for the unit of work it begins, or `UnitOfWorkResolver.From(scopeResolver)`, which
asserts the ambient transaction exists and only then wraps. `From`'s callers are the seams where a
caller-provided scope is proven transactional: the local typermedia navigator (an `IStrictlyLocalTommand` is
`IMustBeSentTransactionally`, asserted before dispatch), the inbox's handler execution task, and the
unit-of-work tevent publisher, which asserts its ambient transaction before routing any delivery leg.

## No unit-of-work lifestyle, deliberately

A lifestyle for components requiring an ambient transaction (`UnitOfWorkParticipant.For<TService>()`:
`Scoped` plus a resolution-time transaction assert, normalized away before the backend containers so they
never saw it) was built 2026-07-16 and deleted the same day — building it was the experiment that disproved
it:

- Only two components could wear it — `IUnitOfWorkTeventPublisher` and `IUnitOfWorkTommandSender` — and
  both already fail loud at first use through stronger guards (the publisher's publish-time assert, the
  sender's `SingleTransactionUsageGuard` plus the validator), which the lifestyle could never replace: it
  proves a component is *born* inside a unit of work, not that it never outlives one.
- Its applicability is intrinsically narrow, because construction context ≠ use context across most of the
  framework: the local typermedia navigator executes tueries from read scopes, `TeventStoreUpdater` and
  `DocumentDbSession` serve reader faces from the same instance — and `TeventStoreUpdater` needed a
  deferred-resolver contortion purely to coexist with the lifestyle.
- The price was public API concept load — an enum member, a builder, and subtle semantics
  (instantiation-time only, presence-not-pairing, a `WithServiceResolver` special case) — for a few
  microseconds of earlier failure on two internal registrations.

Context requirements stay where they were: asserted at use, and stated in signatures by
`IUnitOfWorkResolver`.

## The front-door duality: `UnitOfWork*`/`Session*` / `Independent*`

Application-facing operations come in two flavors, named by the caller's relationship to the execution
context — the same duality `IRootResolver`/`IScopeResolver` expresses at the container level:

| Within the caller's context (Scoped) | As its own context (Singleton, root-resolvable) |
|---|---|
| `IUnitOfWorkTeventPublisher` | `IIndependentTeventPublisher` |
| `IUnitOfWorkTommandSender` (formerly `IServiceBusSession` — a misnomer twice over: no conversational state, and its requirement is the unit of work, not merely the session) | `IIndependentTommandSender` |
| `ILocalTypermediaNavigatorSession` (formerly `IInProcessTypermediaNavigator`) | `IIndependentLocalTypermediaNavigator` (tommands = own unit of work; tueries = own isolated scope) |
| — | `IRemoteTypermediaNavigator`: independent by *nature* — remote typermedia sends are forbidden inside a transaction, so there is no within-the-caller's-context flavor to pair with, and no qualifier |

The left column's prefix is the weakest context the component's whole surface requires. The publisher and
the sender require the caller's **unit of work** — every use demands the ambient transaction. The navigator
requires only the caller's **session** (its scope) — a tuery needs nothing more, and only a tommand
execution additionally demands that the scope be paired with the ambient transaction. Naming the navigator
`UnitOfWork*` would over-claim for tueries; naming the sender `Session*` would under-claim for everything.

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
  usage guards into a unit-of-work lifecycle — stays unplanned: the guards work.
- **A type-level name for the isolated-scope (read) context**, if something ever needs to say it in a
  signature.
