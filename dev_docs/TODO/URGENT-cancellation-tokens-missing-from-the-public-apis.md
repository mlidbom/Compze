# URGENT: cancellation tokens missing from Compze's public APIs

**Urgent.** Put in the backlog by Magnus on 2026-11-27, when the names of item 48's asynchronous navigators and updates
were reviewed: "Add a big fat urgent todo somewhere that we should look through our APIs for missing cancellation tokens
parameters...". Not started.

## Why

Most of Compze's public asynchronous members take no `CancellationToken`, so a consumer can't stop what they wait for:
a request whose caller has gone, or a host that is shutting down. The newest are an example. `ExecuteAsync`, on
`IIndependentLocalTypermediaNavigator` and `ILocalTypermediaNavigatorSession`, and `GetForUpdateAsync` and
`TryGetForUpdateAsync`, on `ITaggregateStoreUpdater`, take none, and neither do the handlers they run.

A rough count from a text search on 2026-11-27: about 110 asynchronous member declarations sit outside the `_internal`
and `_private` namespaces, and about 25 of them take a token. Most of those 25 are in Compze.Tessaging, Compze.Threading
and Compze.Threading.Interprocess. This is a text search, not a count of the public API.

## What to do

Go through every public asynchronous member of every package, the frameworks first: Compze.Tessaging, Compze.Teventive
and its tevent store and taggregate stores, then Compze.Hosting. For each, decide whether it takes a token, then add it
and pass it all the way down to where the waiting happens.

Questions to settle first, with Magnus:

- **Handlers:** should the handlers a navigator or a tessage bus runs receive a token, and whose: the caller's, the
  endpoint's shutdown, or both?
- **Exactly-once delivery:** what does cancelling a handler mean there? Presumably the transaction rolls back and the
  tessage is delivered again, but that has to be walked through the in-order guarantee before anything is built.
- **Locks and units of work:** a wait for a lock, or for another unit of work, as `GetForUpdateAsync` does, gives up when
  its token is cancelled. What does it throw, and how does that relate to the lock's timeout?
- **The convention:** the token as the last parameter, `CancellationToken cancellationToken = default`, as .NET does.
  The analyzers CA2016, which checks that a token is forwarded, and CA1068, which checks that it comes last, can then
  hold the code to it.
