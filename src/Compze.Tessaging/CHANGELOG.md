# Changelog

All notable changes to Compze.Tessaging will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## Unreleased

- Fixed: in-order delivery was lost across a sender restart. Recovery reloaded the undelivered backlog ordered by retry metadata (`RetryCount`, `LastAttemptTime`) — inbox-style thinking misapplied to the outbox — so a tessage stuck retrying at the head of the queue came back last, inverting send order. `GetUndeliveredTessagesForEndpoint` now orders by the outbox tessage table's monotonic `GeneratedId` in every SQL backend, re-establishing head-of-line on the oldest undelivered tessage.
- Named-pipe Tessaging transport: `NamedPipeTessagingTransport()` registers the inbox transport server and the transport client over the same-machine named-pipe transport — cross-process Tessaging with no ASP.NET Core anywhere in the process. The inbox server also answers infrastructure queries, so discovery bootstraps through the inbox address exactly as with HTTP.
- `IInbox` is registered `WithServiceResolver()`: the named-pipe inbox transport server delivers received tessages to the inbox while the inbox owns and starts the server — the constructor cycle is broken by depending on a deferred `IServiceResolver<IInbox>`.
- The in-process bus routes exclusively by wrapper type: `ForTevent<TTevent>` keys an inner tevent type subscription under `IPublisherIdentifyingTevent<TTevent>` and unwraps at delivery, while a subscription to a wrapper type receives the wrapper itself - publisher-conscious subscription (subscribe to `IManagerTevent<IEmployeeTevent>` and receive only the employee tevents a manager published). Subscribing to a tevent type and subscribing to its wrapper receive exactly the same tevents.
- `IInProcessTeventPublisher.Publish` wraps a tevent published without a publisher-identifying wrapper before routing, and the inbox wraps tevents received from the wire the same way (the wire still carries inner tevents until the remote-transport increment).
- `ForTevent` now validates the subscribed type with the subscription rules (interfaces only) instead of the general message-type rules, matching the in-memory dispatcher.
- The tevent-store publishers (`InProcessOnlyTeventStoreTeventPublisher`, `DistributedTeventStoreTeventPublisher`) receive the committed tevent in its wrapper and deliver it wrapped in-process; the distributed publisher hands the outbox the inner tevent until the wire carries wrappers.
- Fixed: registering a second tevent handler whose subscription resolves to an already-registered routing key threw instead of appending the handler.
- The wire carries the fully wrapped tevent: the outbox stores and transmits the wrapper under the closed wrapper type's `TypeId`, endpoints advertise tevent subscriptions in their translated wrapper form, and the router matches wrapped tevents against those advertised wrapper types. Publisher identity crosses endpoints with zero information loss - a remote endpoint can subscribe publisher-consciously (to `IMyTaggregateTevent<IMyTaggregateTevent>`) and receive the wrapped tevent exactly as the taggregate published it. Exactly-once deduplication is unchanged: the dedup identity is the wrapped tevent's own `Id`, carried as transport-envelope data.
- `IOutbox.PublishTransactionally` and `ITessagingRouter.SubscriberConnectionsFor` take the wrapped tevent (`IPublisherIdentifyingTevent<IExactlyOnceTevent>` - covariance makes the wrapped tevent statically exactly-once), so handing them an unwrapped tevent - which no wrapper-typed route would ever match - is a compile error instead of a silent routing no-op.
- The publisher-identifying wrapper carries only publisher identity, not delivery-guarantee markers. `IPublisherIdentifyingTevent<out TTevent>` is the sole wrapper interface; the guarantee lives on the wrapped tevent and is read via covariance. The dedup identity for the exactly-once path is the wrapped tevent's own `Id`, extracted once at the outbox entry and carried as transport-envelope data (so the same delivery/storage path serves both tevent-wrappers and tommands). The earlier `IRemotablePublisherIdentifyingTevent<>`/`IExactlyOncePublisherIdentifyingTevent<>` tiers, which mirrored the inner's guarantee interfaces onto the wrapper, are removed.
- Fixed: an endpoint whose multiple subscriptions matched the same tevent was returned once per matching route by `SubscriberConnectionsFor`, so the outbox saved duplicate dispatching rows for it and delivery failed with a primary-key violation. An endpoint is one subscriber however many of its advertised subscriptions match.

## 0.3.1-alpha

- Fixed packaging: the `_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
