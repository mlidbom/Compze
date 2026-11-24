# Declaring a typermedia API

A typermedia API is made of *resources*: the .NET types its tueries answer with and its tommands return. A resource
carries its data, and also what a caller can do next: the tueries that get related resources, its *links*, and the
tommands that act on it, its *forms*. A caller never assembles a request from an identifier and a URL template: it takes
the link or the form from the resource it holds, and the compiler checks that it exists.

## Links and forms

An account resource, the link that gets it, and the tommands that act on accounts:

[!code-csharp[](declaring-an-api.cs#resource)]

[!code-csharp[](declaring-an-api.cs#tommands)]

`AccountResource.ChangeEmail` is a form handed out already filled in with what the resource knows, the account's id: the
caller supplies only what is its to supply. A tommand that declares a result, as both of these do, answers its caller
with it; one that declares none returns nothing.

A resource crosses between endpoints serialized, so its state is properties the serializer can set, as in the
tessages above. Its links and forms can be methods, which create them from that state.

## The kinds of typermedia tessage

A typermedia tessage is executed: its caller waits for the handler to run and gets its result. Each kind is declared by
inheriting a base class, from `Compze.Tessaging.TessageTypes`:

| Kind | Declared by inheriting | Executed |
|---|---|---|
| Remotable tuery | `Remotable.NonTransactional.Tueries.Tuery<TResult>` | By the endpoint that serves it, for code in other endpoints. |
| At-most-once typermedia tommand | `Remotable.AtMostOnce.AtMostOnceTypermediaTommand`, or `...AtMostOnceTypermediaTommand<TResult>` | By the endpoint that serves it, for code in other endpoints. |
| Strictly-local tuery | `StrictlyLocal.Tueries.StrictlyLocalTuery<TTuery, TResult>` | In its own process: see [Typermedia in one process](in-one-process.md). |
| Strictly-local tommand | `StrictlyLocal.Tommands.StrictlyLocalTommand`, or `...StrictlyLocalTommand<TResult>` | In its own process. |

The TessageBus's exactly-once tommand is not among them: it is sent, and its sender doesn't wait for it to be handled,
see [Tessage types](~/Compze/Tessaging/_docs/tessage-types.md).

## The start

Every API has a front door, the resource a caller starts from. A `NewableResultLink<TResult>` is a tuery that creates its
result itself, with the result type's parameterless constructor, so getting the start takes no round trip:

[!code-csharp[](declaring-an-api.cs#start)]

The start's links and forms lead to everything else the API offers, so a caller that knows `AccountsApi.Start` can reach
all of it.

## Serving the API

An endpoint serves an API by handling its tueries and tommands, each kind in an override of its declaration:

[!code-csharp[](declaring-an-api.cs#serving_endpoint)]

- **A tuery handler gets a scope, and no transaction:** a tuery changes nothing.
- **A tommand handler runs in a unit of work of its own:** its transactional effects commit when it returns, and roll
  back if it throws.
- **Dependencies are parameters,** resolved from the handler's scope: up to two for a tuery handler and one for a tommand
  handler. A handler that needs more takes the scope itself, an `IScopeResolver` or an `IUnitOfWorkResolver`, and resolves
  them from it.
- **Exactly one handler for each type.** Declaring a second one fails when it is declared. Across endpoints, when two
  connected endpoints serve the same type, navigating to it waits for that to resolve, and fails, naming both, when it
  doesn't within the navigating endpoint's patience.

Best-effort and exactly-once endpoints serve typermedia alike. A tuery or tommand crosses between endpoints like any
tessage, so the API's tueries and tommands are mapped to type identities, and both the serving endpoint and the endpoints
that navigate it require those mappings: see [type identities](~/Compze/Tessaging/_docs/endpoints.md#type-identities).
The resources need no mapping.

[Navigating a typermedia API](navigating.md) shows how other endpoints use it.
