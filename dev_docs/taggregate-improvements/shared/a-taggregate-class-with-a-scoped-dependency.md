# A taggregate class with a scoped dependency: Compze names it, whatever the container (end review defect D13)

> Found by CI, 2026-11-18: every CI run since Q3's build (2a05dcbd5) fails one specification, and only with DryIoc. Fixed
> without asking, as the review's other defects were (`reviews/end-review-consolidated.md`, section 1).

## The defect

Q3 ([taggregates-with-dependencies.md](taggregates-with-dependencies.md)) lets a taggregate class of the memory image
state store be declared with a registration of the container, whose factory takes services. The store resolves each
empty instance from the container's root, so the services must be singletons, and a scoped one "fails loudly at the
first resolve". Its specification 5 says the failure names the scoped service.

That name comes from the container, not from Compze. Microsoft's container and Autofac give it; DryIoc says only
"No current scope is available", naming no service. So the specification fails on DryIoc, in every CI run since Q3, on
every persistence layer and transport. Locally the default combination uses only Microsoft's container, so the suite
passed.

## The fix

Compze, not the container, finds and names a scoped dependency, the same for every container:

- **Where Compze can know it, at the earliest point:** the registration's dependencies and their lifestyles are known to
  Compze's container abstraction. If it can check them when the container is built, it refuses there, naming the
  taggregate class, the registration and the scoped service. That is better than at the first resolve: composition fails
  before anything runs.
- **Otherwise at the first resolve,** before the container is asked: Compze checks the registration's dependencies'
  lifestyles itself and throws the same message.
- No catching of the container's exception to translate it: the check comes first, so the container's error never
  arises.

Whichever point the build chooses, the documentation says it: the registrar's and the builder's doc comments, Q3's
document, and the store's changelog line on registrations.

## Specification

Specification 5 of Q3, for every DI container (`[PCT]`): a vocab class declared with a registration whose factory takes a
scoped service fails, at composition or at the first resolve as built, with a message naming the vocab class and the
scoped service. The full suite passes, and the store's specifications pass with each DI container: Microsoft, Autofac
and DryIoc.

## What was built (7045d000e)

**Refused when the container is built,** the earlier of the two points: Compze's container abstraction knows each
registration's dependencies and their lifestyles, so it needed only to be told what the store resolves from the root.

- **Compze.DependencyInjection** (internal, nothing public changes): a component can declare the services it resolves
  from the container's root while it runs, through the `IRootResolver` it takes, with `ResolvingFromTheRoot(...)` on its
  registration. The registration keeps them as `DependencyTypesResolvedFromTheRoot`, and its copies in clone and child
  containers keep them too. `ContainerBuilder.Build()` refuses one that is scoped, or takes a scoped service directly or
  through the tracked transients it takes, before any container is asked. The search ends at a singleton, whose own
  dependencies the existing lifestyle validation checks.
- **The memory image state store's** registration declares the classes declared with a registration.
- **The message** is an `InvalidLifeStyleCombinationException`, the same for every container:

  > Invalid lifestyle combination: Singleton: Compze.Teventive.MemoryImageStateStore._private.TaggregateMemoryImageStateStore
  > resolves Compze.Tests.Integration.CQRS.MemoryImageStateStore.StoredVocab from the container's root, which can't resolve
  > a Scoped service, and its TrackedTransient registration takes
  > Compze.Tests.Integration.CQRS.MemoryImageStateStore.VocabDictionary, which is Scoped.

  Through a tracked transient, the chain is named: "takes ...VocabReadings, a TrackedTransient that takes
  ...VocabDictionary, which is Scoped".
- **Specifications:** `when_the_vocab_class_is_declared_with_a_registration_whose_factory_takes_a_scoped_service` now
  says `building_the_container_fails_naming_the_scoped_service` and `the_failure_names_the_vocab_class`, and a new
  `..._whose_factory_takes_a_tracked_transient_that_takes_a_scoped_service` specifies the chain. They pass, with the
  store's other specifications and Compze.DependencyInjection's, on Microsoft, Autofac, DryIoc and LightInject; the full
  suite passes.
- **The same gap in `StoredInJsonFiles<TService>(...)`, closed the same way (097caf102):** it resolves `TService` from
  the root, for the files' folder, so the registration of `ITaggregateJsonFiles` declares it with
  `ResolvingFromTheRoot(...)`, and `Build()` refuses a scoped one, naming it. Specified by
  `when_the_store_is_stored_in_json_files_in_the_folder_a_scoped_service_gives.building_the_container_fails_naming_the_scoped_service`,
  on all four containers.
