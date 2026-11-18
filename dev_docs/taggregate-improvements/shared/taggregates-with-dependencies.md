# Taggregates with dependencies: declared to the state store through a registration of the container

> Some names and API here were changed by later decisions of the end review (2026-11-18): see
> [the-naming-pass.md](the-naming-pass.md) and [reviews/end-review-consolidated.md](reviews/end-review-consolidated.md).

**Decided by Magnus, 2026-11-18** (the end review's third question, item 68). The state store's overload that built a
taggregate with one service of the container wasn't general enough: "If we support DI for taggregate creation it must
support any shape of constructor regardless of number of services it needs." He asked whether to reuse the DI
abstractions and pass a `ComponentRegistration<TheTaggregateType>`, and approved it ("Go"). Specified here before it is
built.

## What a consumer sees

- **A taggregate class whose instances need services is declared with a registration of the container,** as any
  component with dependencies is:

  ```csharp
  store.Taggregate(TrackedTransient.For<VocabNote>().CreatedBy((Dictionary dictionary, Readings readings) => new VocabNote(dictionary, readings)))
  ```

  `CreatedBy` takes from none to sixteen dependencies, so a constructor of any shape can be declared, and the state store
  has no overloads of its own for them.
- **The store registers it, and resolves a new, empty instance from the container's root** each time it restores one:
  each taggregate at the load and at a reload, and each copy a unit of work gets for update. The dependencies are those of
  the root, so they must be singletons: a taggregate outlives every scope. A scoped one fails loudly at the first
  resolve.
- **Refused at composition, when the store is declared:**
  - a registration whose lifestyle isn't `TrackedTransient`: each instance must be new;
  - a class that is `IDisposable`: a tracked transient resolved from the root is disposed with the container, so the
    container would hold every instance the store ever built.
- **No type arguments:** the store finds the snapshot type from the class's `ISnapshotable<TTaggregate, TSnapshot>`.
  When the store's reload compares snapshots with a comparison of the consumer's, the overload that takes it needs the
  snapshot type written, since a comparison that is a method group can't infer it:
  `store.Taggregate<VocabNote, VocabDto>(registration, CorpusNoteDto.HoldTheSameState)`.
- **The factory without services stays:** `store.Taggregate(() => new StoredVocab())`, for a class that needs none.
- **The overload with one service of the container is removed.**
- **The class becomes resolvable from the container,** as every registered component is: `Resolve<VocabNote>()` gives an
  empty instance that belongs to no store. Known, and accepted.
- **Its documentation says that a taggregate's state must be its own.** Services are for what it does with its own state.
  Reaching other taggregates through a service is what Compze doesn't recommend: a unit of work changes a private copy, so
  whatever a taggregate computes from other taggregates goes stale.

## How it is built

- `StateStoreBuilder.Taggregate<TTaggregate>(ComponentRegistration<TTaggregate> registration)` asserts the lifestyle and
  that the class isn't `IDisposable`, registers the registration with the builder's registrar, finds the snapshot type by
  reflection, and declares the class with a factory that resolves `TTaggregate` from the root resolver the store is built
  with.
- `StateStoreBuilder.Taggregate<TTaggregate, TSnapshot>(ComponentRegistration<TTaggregate> registration,
  Func<TSnapshot, TSnapshot, bool> snapshotsAreEqual)`: the same, with the consumer's comparison.
- The existing assertion that a factory builds instances of exactly the declared class covers a registration whose
  factory builds a subclass.
- `StoredInFiles<TService>` stays as it is: a separate, smaller question.

## Specifications

1. A vocab declared with a registration whose factory takes two singleton services: the copy a unit of work gets for
   update is built with the container's two services, and its change is committed.
2. The same vocab in a store whose files are kept in memory: after a reload, the vocab the store restored was built with
   the container's two services.
3. A registration that isn't a `TrackedTransient` is refused where the store is declared, with a message naming the class
   and the lifestyle.
4. A class that is `IDisposable` is refused where the store is declared, with a message naming the class.
5. A registration whose factory takes a scoped service fails loudly at the first resolve.
6. Everything the factory overloads specified before still holds.
