# Tessage types: the type is the contract

Every tessage is an instance of a .NET type, and that type says everything about it: its kind, who may receive it,
whether it may leave its process, and what its delivery guarantees. The interfaces it implements, all in the
`Compze.Tessaging.TessageTypes` namespace, are how it says so.

## Tevents: interfaces in a hierarchy

A tevent says that something happened. Declare tevents as interfaces that inherit each other, each adding the meaning
that sets it apart:

[!code-csharp[](tessage-types.cs#tevent_hierarchy)]

Classes implement them. Only the code that publishes a tevent knows its class:

[!code-csharp[](tessage-types.cs#tevent_classes)]

### Routing by type

A tevent is delivered to every handler whose subscribed type it is compatible with: the rule is .NET's own type
compatibility, so routing is polymorphism. Given these subscriptions:

[!code-csharp[](tessage-types.cs#routing_by_type)]

| Published | `IAccountTevent` handler | `IAccountRegistered` handler | `IAccountImported` handler |
|---|---|---|---|
| `AccountRegistered` | ✔ | ✔ | |
| `AccountImported` | ✔ | ✔ | ✔ |
| `AccountEmailChanged` | ✔ | | |

That is what makes a hierarchy of tevents worth designing: a subscriber says precisely what it cares about, and keeps
receiving it whatever is added later. A new kind of registration, `IAccountRegisteredBySupport : IAccountRegistered`,
reaches the `IAccountRegistered` subscribers with no change to them. Routing configuration, and type checks in
handlers, have nothing left to do.

> [!TIP]
> Subscribe to interfaces, never to tevent classes. A subscriber to a class misses every tevent of the classes added
> later, and the classes are the publisher's implementation detail to refactor as it likes.

Designing a hierarchy of tevents, for an entity that publishes them, is what
[Teventive](~/Compze/Teventive/Taggregates/_docs/definition.md) is about.

### What a tevent's type promises about its delivery

Three interfaces declare how far a tevent travels and what its delivery guarantees. The publisher and the subscribers
choose none of it:

[!code-csharp[](tessage-types.cs#delivery_contracts)]

| The tevent implements | It travels | Its delivery |
|---|---|---|
| `ITevent` | Within the process that publishes it. | Its handlers there run within the publisher's transaction. |
| `IRemotableTevent` | Also to the endpoints that subscribe to it. | Best-effort: no store, no deduplication, no retries. |
| `IExactlyOnceTevent` | Also to the endpoints that subscribe to it. | Exactly once: stored in the publisher's transaction, deduplicated by its `Id`, its handling retried when it fails. |

[Delivery guarantees](delivery-guarantees.md) says what each guarantee means in full.

## Tommands: exactly one handler

A tommand tells its one handler to do something. The tommand of the TessageBus is the exactly-once tommand: its
handler's transactional effects commit exactly once, wherever the handler is, and the sender doesn't wait for it to be
handled. The
`Remotable.ExactlyOnce.Tommand` base class gives a tommand the `Id` its deduplication needs:

[!code-csharp[](tessage-types.cs#tommand)]

It is sent through `IUnitOfWorkTommandSender` or `IIndependentTommandSender`, which exactly-once endpoints provide: see
[Endpoints](endpoints.md).

Typermedia has tommands of its own, whose callers wait for them to be handled, and may get a result: strictly-local
tommands, executed in-process, and at-most-once typermedia tommands, executed by another endpoint: see
[Typermedia](~/Compze/Tessaging/Typermedia/_docs/introduction.md).

## Tueries: a question with one answer

A tuery asks its one handler for an answer, and changes nothing. Its type declares the type of its result:

[!code-csharp[](tessage-types.cs#tuery)]

Tueries are Typermedia's: see [Typermedia](~/Compze/Tessaging/Typermedia/_docs/introduction.md).

## One handler, and declarations that fail loud

A tommand type and a tuery type have exactly one handler each: declaring a second handler for one fails when it is
declared. A tevent type has any number.

The framework also refuses a type that contradicts itself, such as one that is both a tommand and a tevent, or both
remotable and strictly local. It checks a type the first time the type is sent or subscribed to. To check a whole
vocabulary at once, from a test, point `TessageTypeDesignRules` at the assemblies that declare it:

[!code-csharp[](tessage-types.cs#checking_the_vocabulary)]

It reports every violating type in one failure.
