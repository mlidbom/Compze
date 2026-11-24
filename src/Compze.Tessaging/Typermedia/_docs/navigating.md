# Navigating a typermedia API

Code navigates the APIs that other endpoints serve through `IRemoteTypermediaNavigator`. It gets remotable tueries'
results and posts at-most-once typermedia tommands, each to the one endpoint that serves its type, found the way
endpoints find each other: no addresses, no routes, nothing to configure.

## The navigating endpoint

The navigator belongs to an endpoint. This one serves nothing itself: it navigates the accounts API of
[Declaring a typermedia API](declaring-an-api.md), and requires the type mappings of its tueries and tommands:

[!code-csharp[](navigating.cs#navigating_endpoint)]

Both endpoints are hosted here in one process, in the environment of the [Endpoints](~/Compze/Tessaging/_docs/endpoints.md#the-environment)
page. In separate processes, the code is the same: see [Processes on one machine](~/Compze/Tessaging/_docs/on-one-machine.md).

[!code-csharp[](navigating.cs#hosting)]

The navigator is a singleton, so an application's own classes take it as an ordinary constructor dependency.

## One step at a time

`GetAsync` gets a tuery's result, `PostAsync` posts a tommand and returns its result, and each has a synchronous form,
`Get` and `Post`:

[!code-csharp[](navigating.cs#one_step_at_a_time)]

Each step takes its tuery or tommand from the resource the step before returned. Getting the start takes no round trip:
it creates its result itself.

## Navigation specifications

A `NavigationSpecification` describes a whole navigation as one value, step by step, and runs nothing until it is
navigated:

[!code-csharp[](navigating.cs#navigation_specification)]

- **`NavigationSpecification.Get(tuery)` starts at a tuery,** and `tommand.Post()` at a tommand.
- **`Get` follows a link, and `Post` submits a form,** each built from the result of the step before.
- **`Select` takes what the navigation is after** from the result of the step before, with no round trip:

[!code-csharp[](navigating.cs#get_and_select)]

A specification navigates on any remote navigator, with `navigator.NavigateAsync(specification)`,
`navigator.Navigate(specification)` or `specification.NavigateOn(navigator)`, and can be navigated as often as needed. So
an API can publish its common navigations as specifications, for its clients to navigate as they are or to extend with
more steps. A specification whose last step posts a tommand that returns nothing navigates only synchronously, with
`NavigateOn`.

## What a navigation guarantees

- **It waits for a server.** A navigation to a type that no connected endpoint serves waits for one, within the
  navigating endpoint's handler-availability patience, and a navigation to a type that two connected endpoints serve
  waits for that to resolve: see [Readiness and waiting sends](~/Compze/Tessaging/_docs/readiness-and-waiting-sends.md).
- **Never within a transaction.** Navigating from code within a transaction fails at once: a remote tuery or tommand
  can't take part in the caller's transaction, so it never seems to.
- **Only other endpoints.** An endpoint's navigator reaches the APIs that other endpoints serve. A type that only the
  navigating endpoint serves is never found: navigating to it waits, then fails as if nothing served it.
- **A handler's failure reaches the caller.** When a handler throws, the navigation fails with
  `TessageDispatchingFailedException`, whose message holds the handler's exception, as text: its type doesn't cross.
- **A tommand's transactional effects commit at most once for each post.** A tommand handler that throws is run again at
  once, in a new unit of work, up to five times in all. Its effects outside the transaction happen again with each try,
  such as an email sent. A tuery handler that throws is not run again.
- **Nothing deduplicates posts.** Posting the same tommand twice executes it twice. The navigator never posts again by
  itself, so when a post fails on its way, the caller can't tell whether the tommand was executed.
