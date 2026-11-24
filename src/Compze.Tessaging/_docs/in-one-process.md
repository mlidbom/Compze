# Tessaging in one process: the LocalTessagingEngine

Many applications need Tessaging only inside one process: tevents that decouple the parts of an application from each
other, published by one part and handled by the others. For that, a container composes the LocalTessagingEngine, and
nothing else: there is no endpoint, no transport and no host, and everything but observation runs on the caller's
thread.

## Composing the engine

The engine is composed into a container in one block, which declares every handler it will ever have:

[!code-csharp[](in-one-process.cs#usings)]

[!code-csharp[](in-one-process.cs#composing_the_engine)]

- **One engine per container.** Composing a second one fails.
- **The declaration is the only way in.** The engine's handlers are fixed when the container is built: nothing can
  register a handler later.
- **Handlers take what they need.** A handler takes the tevent, and up to two more parameters, which are resolved from
  the scope of the unit of work that delivers the tevent. A handler can be synchronous, or return a `Task`.

The same declaration block is what an endpoint's handlers are declared through, so these handlers run unchanged when
the application later becomes several [endpoints](endpoints.md).

## Publishing tevents

Two interfaces publish tevents. Which one depends on whether the code runs inside a *unit of work*: a scope of the
container paired with a `System.Transactions` transaction, begun together and completed together, so that all its
effects commit or roll back as one.

**Outside any unit of work,** such as in an application's own classes, publish through `IIndependentTeventPublisher`.
It is a singleton, so a class takes it as an ordinary constructor dependency, and each publish runs as a unit of work
of its own:

[!code-csharp[](in-one-process.cs#application_class)]

[!code-csharp[](in-one-process.cs#publishing_independently)]

It refuses to publish within an ambient transaction, since its publish would join that transaction instead of standing
on its own.

**Inside a unit of work,** such as in a handler, publish through `IUnitOfWorkTeventPublisher`. A handler takes it as one
of its parameters; code that begins a unit of work itself resolves it from the unit of work:

[!code-csharp[](in-one-process.cs#publishing_in_a_unit_of_work)]

It refuses to publish without an ambient transaction, since there is no unit of work to publish within.

Both have a `Publish` and a `PublishAsync`. `PublishAsync` publishes every kind of tevent; `Publish` refuses an
`IExactlyOnceTevent`, whose publishing writes to a database within the transaction.

## What publishing does

In the publishing process, a published tevent's handlers *participate* in the publish:

- **They run before `Publish` returns,** on the publisher's thread, within the publisher's transaction.
- **They run in the order they were declared,** whichever types they subscribe to, so a handler may rely on what the
  handlers declared before it did.
- **A handler that throws fails the publish.** The exception reaches the publisher, and the unit of work rolls back,
  with the effects of the handlers that ran before it.
- **A handler may publish tevents in turn,** through the `IUnitOfWorkTeventPublisher` it takes as a parameter; their
  handlers participate in the same unit of work.

That makes a handler part of the publisher's change: the publisher and its handlers commit together or not at all. A
reaction that should happen only once the change has committed, and should never fail it, is an
[observer](observing-tevents.md) instead.

## Stopping

Stop the engine before disposing the container:

[!code-csharp[](in-one-process.cs#stopping_the_engine)]

Stopping waits, holding no thread, for the observers to observe every tevent committed so far, and then refuses new
publishes. Disposing a container that wasn't stopped stops the engine first.

## What a container without endpoints doesn't have

A tevent published in a container with no endpoint reaches the handlers in that container, and only them. Sending an
exactly-once tommand takes an exactly-once [endpoint](endpoints.md): `IUnitOfWorkTommandSender` and
`IIndependentTommandSender` are registered by those endpoints, not by the engine.

The engine also executes strictly-local tueries and tommands, through `ILocalTypermediaNavigatorSession` and
`IIndependentLocalTypermediaNavigator`: see [Typermedia](~/Compze/Tessaging/Typermedia/_docs/introduction.md).
