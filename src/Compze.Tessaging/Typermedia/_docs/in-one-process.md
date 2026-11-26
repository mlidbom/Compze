# Typermedia in one process

An API needn't cross processes. Strictly-local tueries and tommands are executed in the process that declares their
handlers, starting on the caller's thread: nothing is serialized and no endpoint is needed. The LocalTessagingEngine of
[Tessaging in one process](~/Compze/Tessaging/_docs/in-one-process.md) executes them.

## Strictly-local tueries and tommands

An application's account settings, as a resource with two forms:

[!code-csharp[](in-one-process.cs#strictly_local_tessages)]

A strictly-local tuery names its own type as its first type argument. Strictly-local tueries and tommands never leave
their process: the remote navigator doesn't take them. So they need no type identities, and their resources need not be
serializable.

## Declaring the handlers

Typermedia handlers are declared in the engine's composition block, under a verb of their own beside
`RegisterTessageBusHandlers`:

[!code-csharp[](in-one-process.cs#composing)]

A tuery handler takes up to four more parameters and a tommand handler one, resolved from the scope that executes it. In
an endpoint, the same handlers are declared in its declaration's `RegisterTueryHandlers` and
`RegisterTypermediaTommandHandlers` overrides.

## Executing them

Two navigators execute them, as two publishers publish tevents. Which one depends on whether the code runs inside a
unit of work.

**Outside any unit of work,** such as in an application's own classes, execute through
`IIndependentLocalTypermediaNavigator`. It is a singleton: a class takes it as an ordinary constructor dependency. It
executes each tommand as a unit of work of its own, committed when the call returns, and each tuery in a scope of its
own, with no transaction:

[!code-csharp[](in-one-process.cs#executing_independently)]

It refuses to execute within an ambient transaction, where its execution would join that transaction instead of
standing on its own.

**Inside a unit of work,** such as in a handler, execute through `ILocalTypermediaNavigatorSession`, resolved from the
unit of work. A tommand executes within the caller's transaction, and commits or rolls back with it; executing one with
no transaction fails. A tuery executes in the caller's scope, so what it reads is consistent with what the caller has
written:

[!code-csharp[](in-one-process.cs#executing_in_a_unit_of_work)]

A handler's exception reaches the caller as it was thrown, and the handler isn't run again.

## Executing asynchronously

Each navigator has `ExecuteAsync` beside each `Execute`, with the same semantics, for callers that are asynchronous
themselves, such as an ASP.NET Core route or an asynchronous handler. A tommand's unit of work commits before the task
completes, and the caller's transaction flows across the handler's awaits:

[!code-csharp[](in-one-process.cs#executing_asynchronously)]

While a handler awaits, `ExecuteAsync` holds no thread, where `Execute` holds the caller's thread until the handler has
finished. What the handler does synchronously runs on the thread it is on either way, and so does a tommand's commit:
System.Transactions commits synchronously. A handler that completes without awaiting holds its thread for its whole run
through either.

## Local or remote is the type's

A tuery or tommand is either strictly local or remotable, and each navigator takes only its own kind: the compiler
refuses a remotable tuery on a local navigator, and a strictly-local one on the remote navigator. So moving an API out of
its process changes its types, not only where it is deployed, and navigation specifications, which navigate remote
navigators only, have no in-process counterpart.
