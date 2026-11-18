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
