# Observing tevents

A handler takes part in the change that published its tevent: it runs within the publisher's transaction, and its
failure fails the publish. Many reactions should do neither. A view that refreshes, a read model that updates, a
notification: each reacts to what has happened, and the change that happened shouldn't wait for it, nor fail because
of it. Such a reaction is an *observer*. It watches tevents that have been committed, and stands outside every
transaction.

## What an observer can rely on

- **Committed tevents only.** A tevent is queued for its observers when the unit of work that published it commits. A
  tevent whose unit of work rolls back is never observed.
- **Off the publisher's thread.** Observers run on threads of their own: an observer's slowness never delays the
  publisher, and its failure never reaches it.
- **In order.** Each observer has a queue of its own, and observes tevents in the order they were committed. A slow
  observer delays only itself.
- **Once.** An observer never observes a tevent twice.
- **No transaction.** Each call gets a fresh scope of the container, with no transaction.
- **Failures are reported, not retried.** A throwing observer's exception is logged as an error, and the observer goes
  on with its next tevent.

Observers and handlers run independently of each other, so nothing orders an observer's call relative to the
handlers of an arriving tevent.

## Composed observers

Observers that live as long as the container are declared next to the handlers, under a verb of their own:

[!code-csharp[](observing-tevents.cs#composed_observers)]

`ForTeventsOfEachUnitOfWork` observes the tevents of each committed unit of work as one list, in the order they were
published: for a reaction that is costly for each call, such as refreshing a view or writing a read model to a
database. It is never called with an empty list. A tevent with no unit of work known to its process, one published
through `IIndependentTeventPublisher` or one that arrives from another endpoint, comes as a list of its own.

Like a handler, an observer takes up to two more parameters, resolved from its scope.

## Dynamic observers

Some observers come and go while the application runs, such as one for each open view. `IDynamicTeventObservers`
creates them, and disposing what it returns ends them:

[!code-csharp[](observing-tevents.cs#dynamic_observer)]

A dynamic observer observes the tevents committed after `ObserveTevents` returns. Disposing it drops what is queued
for it and not yet started, and doesn't wait for a call that is already running, so a view disposed on its UI thread
can't deadlock with an observer that waits for that thread.

## Waiting for the observers

A batch job or a tool that reports, exports or backs up once the observers have caught up waits for them:

[!code-csharp[](observing-tevents.cs#awaiting_the_observers)]

It waits, holding no thread, until every tevent committed so far has been observed by every observer of it, including
what the observers publish meanwhile. Tevents that other code publishes during the wait may or may not be waited for,
so it is for code that controls what runs.

## Observers in an endpoint

An [endpoint](endpoints.md) declares composed observers by overriding `ObserveTevents`, and they observe the tevents
that arrive from other endpoints as well as those committed in its own process. An arriving tevent was committed by its
publisher, so it is queued for its observers as it arrives.

An endpoint asks the other endpoints for the tevents it subscribes to when it starts. So a dynamic observer in an
endpoint may observe only types the endpoint subscribed to for it, in its declaration's `Declare` override:

[!code-csharp[](endpoints.cs#subscribing_for_dynamic_observers)]

Observing a remote exactly-once tevent takes an exactly-once endpoint: it is received exactly once, through the
endpoint's inbox, like any subscription to it.
