# Typermedia APIs

The most popular API in the world is a hypermedia API. You're using it right now - it's called the World Wide Web. Can
you imagine trying to use it without links and forms? Instead being given an int or a string? Why do we build APIs like
that?

Compze extends hypermedia into Typermedia which:

- **Routes tueries and tommands by their .NET types**, giving:
    - Zero configuration routing
    - A simple, already well-known programming model
- **Can be fully explored**, browsed much like a website, by:
    - Getting links: tueries
    - Posting forms: tommands
    - All with full type safety and autocomplete in your IDE
- **Further encapsulates your domain**, exposing less implementation details than traditional APIs
    - All domain logic, validation, and available actions can be encapsulated in the .NET types returned by the API
    - The UI need only bind these types to UI components without implementing any domain logic
    - Application behavior can be fully tested without any UI framework
    - Changing the UI becomes far less burdensome and does not risk changing domain logic

> [!NOTE]
> Typermedia doesn't require distribution. An API whose tueries and tommands are strictly local is executed in its own
> process, synchronously, on the caller's thread: see [Typermedia in one process](in-one-process.md). Distribution and
> asynchronous communication are optional, not a requirement.

> [!NOTE]
> In-process, the performance overhead is negligible in the great majority of systems: dispatching essentially comes
> down to a lookup in a dictionary, with the tessage's type as the key.

## A first taste

An account, as an accounts API returns it, carries its data and a form for changing it:

[!code-csharp[](declaring-an-api.cs#resource)]

A client navigates from the API's start, opening an account and then changing its email, each step taking its tommand
from the resource the step before returned:

[!code-csharp[](navigating.cs#navigation_specification)]

A developer could write all of that without ever leaving their IDE. Autocomplete in the IDE makes the API browsable, not
just at runtime, but as part of writing code. The same goes for all of the functionality of a domain exposed through a
Typermedia API, not just the simple example above.

Once you've used APIs like this, how would you feel about an API that gives you an `int` instead of an `AccountLink`?

## The pages, in reading order

Typermedia is one of the two communication styles of [Tessaging](~/Compze/Tessaging/_docs/introduction.md), and these
pages build on Tessaging's: tueries and tommands are tessages, and remote APIs are served and navigated by
[endpoints](~/Compze/Tessaging/_docs/endpoints.md).

1. [Declaring a typermedia API](declaring-an-api.md): resources, their links and forms, the start, and the endpoint
   that serves them.
2. [Navigating a typermedia API](navigating.md): the remote navigator, navigation specifications, and what a navigation
   guarantees.
3. [Typermedia in one process](in-one-process.md): strictly-local tueries and tommands, and the navigators that execute
   them.

Typermedia is part of the Compze.Tessaging package; its tessage types are in Compze.Tessaging.Abstractions.
