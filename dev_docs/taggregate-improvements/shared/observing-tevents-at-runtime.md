# Observing tevents at runtime, and a unit of work's tevents as one list: specified

Two additions to Compze.Tessaging's observation, approved by Magnus on 2026-11-17. They came from JAStudio's move from
its `NoteUpdated` observable to tevents, and from the same need in Deskmancer.

- **Runtime observation:** an observer created while the application runs, such as by a view, and ended by disposing
  what creating it returned.
- **Unit-of-work batches:** an observer that gets the tevents of each committed unit of work as one list.

All names here are placeholders for the naming pass of the spike's end review.

## Why Compze, by the rules on consumers' requests

- **Runtime observation** is the bus's own capability, given a lifetime shorter than the container's. Any consumer with
  views, connections or short-lived waits needs it. JAStudio's view models come and go with the UI. Deskmancer's
  black-box specifications register an await for one tevent before a keystroke. A consumer can't do it as well itself:
  both had to build a hub that routes each tevent to the subscribers whose types it's compatible with, and so
  re-implement Compze's routing by type outside Compze. Magnus was sceptical of such hubs: "when you start to not use
  the bus to dispatch things and start to build your own messaging paths, I start looking skeptical."
- **Unit-of-work batches** serve anyone whose reaction is costly per call: a view refreshing, a read model writing to a
  database, a notification sent. A consumer can't do it as well itself: nothing on a tevent marks where its unit of work
  ends, so an observer could only guess by time.

## Observation only: no participation at runtime

Magnus, 2026-11-17: "Maybe this should only be allowed for observation. If it should be allowed for
subscribing/participation is indeed questionable." Neither consumer needs participation. Deskmancer's agent-rep: "A
failing awaiter must not touch the application under test, so participation would be wrong here." JAStudio's view models
observe. A participant added at runtime would run inside every publisher's transaction, holding its locks, and its
failure would roll back the domain's change.

## Runtime observation

```csharp
public interface ITeventObservation   //review-naming: a placeholder
{
   IDisposable ObserveTevents(Action<TeventObservationRegistrar> observe);
}
```

- **Resolved from the container,** as a singleton, in a plain container's composition and in an endpoint's alike.
- **The same registrar as composition's `ObserveTevents`,** so a runtime observer is declared exactly as a declared one
  is, with every overload, and is routed by type in the same way.
- **The same semantics as a declared observer:**
  - committed tevents only;
  - off the publishing thread;
  - in commit order, in a queue of its own;
  - a fresh scope for each call, with no transaction;
  - a throwing observer is reported to the background-exception reporter, and its queue goes on;
  - `ILocalTessagingEngine.AwaitNoTessagesInFlightAsync` and `StopAsync` wait for it too.
- **What it observes:** every tevent queued for observation after `ObserveTevents` returns. A unit of work that commits
  while the call runs may or may not be observed.
- **Disposing ends it:**
  - no call to the observer starts after `Dispose` returns;
  - what is queued for it and not yet started is dropped, and no longer counts as in flight;
  - `Dispose` doesn't wait for a call already running, so a view disposed on the UI thread can't deadlock with an
    observer that is waiting for the UI thread;
  - it is safe from inside the observer's own call;
  - disposing twice does nothing more.
- **Refused once the engine has stopped,** as publishing is: nothing would be observed.
- **The list of declared handlers stays closed at build,** as `LocalTessagingEngineBuilder` says it is. Runtime observers
  are kept in a list of their own, so nothing checked at build changes.

### What may be observed at runtime is declared at composition

An endpoint receives a remote tevent only of the types it declared handlers for at composition: it tells its peers what
to send it. Deskmancer needs remote tevents: its black-box specifications register an await for one analytics tevent
before a keystroke, and the application under test runs in another process. Approved by Magnus, 2026-11-17, built after
the rest of this document:

```csharp
.ObserveTevents(observe => observe.AtRuntime<IAnalyticsEvent>())   //review-naming: a placeholder
```

- **`AtRuntime<TTevent>()` declares that tevents compatible with `TTevent` may be observed at runtime,** subtypes
  included. It is a member of `TeventObservationRegistrar` and of `ITeventObservationRegistrar`, used only at
  composition: in a runtime registration it fails.
- **An endpoint subscribes with its peers to the declared types from the start,** as it does for a declared observer.
  A runtime observer is in effect the moment `ObserveTevents` returns, with no round trip, so an await registered
  before a trigger can't miss a tevent published right after it. Deskmancer's agent-rep: the register-before-trigger
  requirement is what this must meet.
- **An arriving tevent of a declared type that no runtime observer observes is dropped.**
- **Every runtime observer's types must be covered by an `AtRuntime` declaration,** in a plain container too, asserted
  when `ObserveTevents` is called. The rule is the same in every composition, as the engine builder's principle has it:
  "an application's handler registrations run unchanged under any composition". A runtime observer then never silently
  misses the remote tevents of a type nobody declared. So what may get into the engine is still all declared at
  composition, and only the observers come and go. (jastudio-rep-opus's call: JAStudio declares
  `observe.AtRuntime<INoteTevent>()`, a line.)
- **Specifications, across two endpoints:** a runtime observer in one endpoint observes a tevent published in the other,
  of a declared type, and stops when disposed; one registered before the publish observes it however soon the publish
  follows; a runtime observer of an undeclared type is refused; `AtRuntime` in a runtime registration fails.

## Unit-of-work batches

```csharp
observe.ForEachUnitOfWork<INoteTevent>(tevents => ...)   //review-naming: a placeholder
```

- **A member of `TeventObservationRegistrar`,** with the same overloads as `ForTevent`: a delegate taking the list and
  the scope, sync or async, and the convenience overloads that resolve dependencies from the scope. It is also on
  `ITeventObservationRegistrar`, the facet an endpoint declaration receives. So it can be declared at composition and
  at runtime.
- **What the observer gets:** an `IReadOnlyList<TTevent>` with the tevents of one committed unit of work that are
  compatible with `TTevent`, unwrapped, in the order they were published. It is called once for each committed unit of
  work that published any, and never with an empty list.
- **The rest is as for any observer:** committed only, off the publishing thread, in commit order, in a queue of its
  own.
- **Where a unit of work isn't known, the list holds one tevent:**
  - a tevent published through `IIndependentTeventPublisher`, which is its own unit of work anyway;
  - a tevent that arrives from another endpoint: its sender's unit of work isn't marked on it.
- **No information about the unit of work,** by the rule that the frameworks keep their public surface minimal. Add one
  when a consumer names a need.
- **How:** `UnitOfWorkTeventPublisher` registers a hook for each tevent with `OnCommittedSuccessfully` today. Instead, a
  unit of work collects its published tevents in publish order, and one hook at the commit queues them: each tevent for
  the per-tevent observers, as today, and the matching list once for each batch observer. A tevent a participation
  handler publishes in response is still collected after its cause, so observers still see cause before consequence.

## Specifications

- **A runtime observer:**
  - observes a tevent committed after it was created;
  - doesn't observe a rolled-back one;
  - stops observing when disposed;
  - can be disposed from its own call;
  - is waited for by `AwaitNoTessagesInFlightAsync`;
  - is observed by type compatibility, as declared observers are;
  - an observer that throws doesn't stop its later calls.
- **A batch observer, declared and at runtime:**
  - gets a unit of work's three tevents as one list, in publish order;
  - gets only the tevents compatible with its type;
  - isn't called for a unit of work with no compatible tevent;
  - gets two units of work as two lists.
- **The engine refuses a runtime observer once stopped.**
