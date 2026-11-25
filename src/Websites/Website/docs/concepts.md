# Concepts

Compze's libraries share a small vocabulary. Most of its words are familiar ones with a T in front: a *tevent* is an
event, a *taggregate* an aggregate. This page defines each of them, in the order they build on each other, and links to
where each is explained in depth.

## What Compze builds on

These terms are not Compze's own. Most come from [Domain Driven Design](https://www.goodreads.com/en/book/show/179133.Domain_Driven_Design),
which [Martin Fowler summarizes](https://martinfowler.com/bliki/DomainDrivenDesign.html) as well as anyone can.

### Entity
An object defined by its identity, which stays the same while its attributes change: a user is the same user after
changing their email.

### Aggregate
A cluster of domain objects that is changed as one unit, only through its root, which keeps the cluster's rules true.
A user, with their profile and their addresses, can be one. See [Fowler's description](https://martinfowler.com/bliki/DDD_Aggregate.html).

### Event, command and query
Three kinds of message. An *event* says that something happened. A *command* asks for something to be done. A *query*
asks for information, and changes nothing.

### Routing
How a message reaches the code that handles it. Most messaging libraries route by names configured beside the
message: a topic, a queue, a routing key.

## Type-routed messages

### Tessage
A message routed by its .NET type. The type is the tessage's whole contract: there are no topic names, queue names or
routing keys to configure, and its type also declares whether it may leave its process, and what its delivery
guarantees. A tessage reaches the handlers whose type it is compatible with, by .NET's own rules of type compatibility.

There are three kinds of tessage, the three kinds of message above: tevents, tommands and tueries.
[Tessaging](~/Compze/Tessaging/_docs/introduction.md) sends and delivers them, and
[Tessage types](~/Compze/Tessaging/_docs/tessage-types.md) shows how each is declared.

### Tevent
A tessage that says something happened: a type-routed event. It reaches every handler subscribed to a type it is
compatible with: none, one or many.

Tevents are declared as interfaces that inherit each other, each adding meaning to the ones it inherits. So the
subscribers to `IUserRegistered` also receive an `IUserImported`, declared as `IUserImported : IUserRegistered`,
including the kinds of registration added after they were written. [Tevents](~/Compze/Teventive/Taggregates/_docs/definition.md)
defines them in full.

### Tommand
A tessage that tells its one handler to do something: a type-routed command. Its sender either doesn't wait for it to
be handled, the TessageBus's exactly-once tommands, or waits, and may get a result, Typermedia's tommands.
See [Tessage types](~/Compze/Tessaging/_docs/tessage-types.md#tommands-exactly-one-handler).

### Tuery
A tessage that asks its one handler a question, and changes nothing: a type-routed query. Its type declares the type
of its answer: a tuery that answers with an account is an `ITuery<AccountResource>`.
See [Declaring a typermedia API](~/Compze/Tessaging/Typermedia/_docs/declaring-an-api.md).

## Teventive domain models

### Teventive
Changing state only by applying tevents. A teventive class doesn't set its state directly: it publishes a tevent that
says what happened, and the tevent's applier changes the state. Applying the same tevents again rebuilds the same
state. *Teventive programming* models a domain this way, in tevents whose types say what they mean.

### Taggregate
An aggregate that is teventive: its state changes only by applying the tevents it publishes, and every tevent it
publishes carries its id. See [Taggregates](~/Compze/Teventive/Taggregates/_docs/taggregates.md), and
[Taggregate inheritance](~/Compze/Teventive/Taggregates/_docs/taggregate-inheritance.md) for taggregates that inherit
each other.

### Tomponent
A component of one taggregate, with state of its own, teventive like the taggregate. Its tevents are part of the
taggregate's tevent hierarchy, carry the taggregate's id, and reach subscribers as the taggregate's.
See [Tomponents](~/Compze/Teventive/Taggregates/_docs/tomponents.md).

### Tentity
An entity of one taggregate: a part with an id of its own, of which the taggregate can hold any number, teventive like
the taggregate. Its tevents are the taggregate's, as a tomponent's are, and also carry its own id.
See [Tentities](~/Compze/Teventive/Taggregates/_docs/tentities.md).

### Shared tomponent
A tomponent written once, and used in many different taggregates without duplicating its code.
See [Shared tomponents](~/Compze/Teventive/Taggregates/_docs/shared-tomponents.md).

### Shared tentity
A tentity written once, and kept in a collection by many different taggregates.
See [Shared tentities](~/Compze/Teventive/Taggregates/_docs/shared-tentities.md).

### Taggregate store
Where taggregates are kept. A *tevent store* keeps every tevent a taggregate has published, and rebuilds the taggregate
by applying them again. A *memory image state store* keeps the taggregates' state.

## Compze's libraries

### Teventive
Teventive programming: tevents, taggregates, and the stores that keep them. Start with
[Tevents](~/Compze/Teventive/Taggregates/_docs/definition.md).

### Tessaging
Compze's messaging: it sends and delivers tessages, within one process and between endpoints. Its two communication
styles are the *TessageBus*, which publishes tevents and sends tommands without waiting for them to be handled, and
Typermedia. Start with [Tessaging](~/Compze/Tessaging/_docs/introduction.md).

### Endpoint
What lets tessages cross between the parts of a system that don't share a container: handlers of its own, a transport
through which the other endpoints reach it, and an identity that stays the same across processes and restarts.
See [Endpoints](~/Compze/Tessaging/_docs/endpoints.md).

### Typermedia
APIs that are browsed much like a website. A typermedia API's tueries are its links, its tommands are its forms, and
the resources they return carry the links and forms that lead on. Start with
[Typermedia APIs](~/Compze/Tessaging/Typermedia/_docs/introduction.md).
