# Bugs and smaller findings

**Status: draft, under review by the four sessions** (2026-11-16). Each was reproduced against Compze.Teventive
0.6.0-alpha unless it says otherwise. None is marked at the code with a `bug:` comment: the work so far is design, with
no change to Compze.

## Two bugs in Compze.Teventive, wrong today whatever JAStudio does

### `Taggregate.Publish`: an observer that publishes on the same taggregate

At the end of a publish cascade, `Publish` enumerates the cascade's list with a `foreach` and notifies
`TeventStream`'s observers, then clears the list. An observer that publishes on the same taggregate from inside its
notification makes the inner `Publish` add to the list under the enumeration, and notify the outer cascade's tevents a
second time.

- **An observer that publishes once:** it is notified of the outer tevent twice, then `Publish` throws
  `InvalidOperationException` ("Collection was modified") out of the domain method, after the state has changed.
  Reproduced by both teams.
- **An observer that publishes every time it is notified of the tevent it reacts to:** each inner `Publish` notifies it
  of the same tevent again, without end. The process dies with a stack overflow (exit code 0xC00000FD), which nothing
  can catch. Found by taggregates-fable, reproduced by jastudio-rep-fable.
- **Fixed by** the queue described in [the-design.md](the-design.md): a publish from inside a notification is handed
  over after the call in progress, in the order published.

### `ITaggregate.Commit`: a publish from inside the commit callback

`Commit` hands its callback the live list of uncommitted tevents, and clears it after the callback returns.

- **If the callback is enumerating the tevents,** a publish on the same taggregate from inside it throws
  `InvalidOperationException`.
- **If it isn't, the tevent is lost without a word.** The publish adds it to the list, and the `Clear` removes it: the
  state has changed and the version advanced, and no `Commit` ever hands it over. A tevent store would hold a history
  missing that version.
- **Fixed by** taking the list out before calling back: the tevent published from inside stays uncommitted for the next
  `Commit`.

Both fixes, with specifications, are on `taggregates-opus/snapshot-and-hook`.

## What an exception leaves behind today

A tevent applier or a tevent handler that throws leaves the tevent applied, or half applied, and uncommitted, with the
invariants never asserted. The next publish then notifies and commits both tevents, as if nothing had happened. A tevent
store throws the instance away with its unit of work; a consumer whose taggregates live on, as JAStudio's do, can't
tell. The proposal, for Magnus to decide, is in [the-design.md](the-design.md): such a taggregate refuses further use.

## Eight behaviours JAStudio's notes rely on, which no specification in Compze pins

Checked by taggregates-fable against 0.6.0-alpha, all holding. Each is a candidate for a specification:

1. One tevent that two components apply, because its interface inherits an interface of each.
2. A subscription to a meaning that several tevents share.
3. A base class's tevent applier in a subclass, its tevent told apart by the subclass's wrapper.
4. Two shared tomponents of one class in a component, told apart by their slots, one of them of a subclass.
5. A subscription to an owner's interface that a slot's adopting wrapper tevent implements, which receives what is
   adopted through that slot, and nothing from another.
6. A subscription to a shared tomponent's own tevent type, which receives nothing of what is adopted.
7. A generic shared tomponent with generic tevents, in a slot of the root.
8. A generic tevent interface that isn't a wrapper, matched by covariance. For a struct as its type argument it matches
   nothing, without an error.

## What the order of tevent appliers is relied on for

All matching tevent appliers are called in the order declared, and three things depend on it today. Routing per class
keeps the rule:

- **An entity's collection:** it creates the entity, routes the tevent to it, and removes a removable entity, in that
  order.
- **A base class's tevent appliers run before a subclass's.**
- **Every tevent applier in the tree runs before any tevent handler.**

## Smaller findings in Compze

- **`Compze.Core.slnx` doesn't build on `main`.** It still lists `src/SolutionStructure/Solution.dev_docs.csproj`,
  which commit `048eb6719` deleted on 2026-07-23, so `dotnet build` fails with MSB3202 before compiling anything. No other
  solution lists it.
- **A filtered `dotnet test` can print "Passed!" after its test host crashes.** When the host dies, of a stack overflow
  for instance, the run is reported as failed, but the assembly's summary line can still read "Passed!" with fewer tests
  in it. Compare the totals with the previous run. `C-Test` could fail when a test assembly's host exits abnormally.
- **`ITaggregateCreatedTevent`'s doc comment names `SingleTaggregateQueryModelUpdater`,** which doesn't exist. The class
  it means is `SingleTaggregateQueryModelGenerator`.
- **The repository's root `README.md` says that "in memory performance overhead is entirely negligible".** That is true
  per dispatch, not per instance held.
- **`TaggregateTypeValidator` scans whole assemblies, three times per class,** in each taggregate, component and entity
  class's static constructor. Once per assembly would do.
- **`PublisherTevent.WrapIn` allocates a closure on every call,** even when the wrapper's constructor is cached:
  `Constructor.GenericTypeConstructor.WithArgument` passes `GetOrAdd` a lambda that captures the argument type
  (`src/Compze.Internals.SystemCE/ReflectionCE/Constructor..cs`). About 90 B of garbage for every tevent published or
  loaded, on `main` today. A `TryGetValue` before the `GetOrAdd` removes it. Found by jastudio-rep-fable from its
  measurements, confirmed by reading.
- **The prototypes' loading allocates about twice what it needs:** 4,192 B for a vocab of five tevents in the combined
  prototype, against 2,040 B in the standalone one. The difference, by reading: `ScopedChange.Enter` in `ApplyTevent`
  (two delegates and a `Disposable` per tevent), `Argument.Assert` calls in the loop whose message lambdas capture the
  tevent (a closure and two delegates per tevent, whether or not the assertion fails), a copy of the snapshot into a
  list, and the wrapper. For a vocab of about eleven tevents that is about 9 KB of garbage per vocab at each start. The
  real implementation should avoid the closures, and the performance specification's bound on allocated bytes will hold
  it to that. Found by jastudio-rep-fable.
- **Per published tevent today:** `ScopedChange.Enter` twice, each two delegates and a `Disposable`; a new wrapper at
  each component level, through `Constructor.ForGenericType`; and a `TessageId` with two readings of the clock.
- **A `TeventDispatcherConfig` handed to a base constructor is built again for every instance,** though it is always a
  constant of the class.

## In JAStudio

taggregates-fable reports what the two JAStudio sessions found in JAStudio itself, such as a stale cache in
`VocabNoteForms`, in JAStudio's `dev_docs/taggregates/shared/`.
