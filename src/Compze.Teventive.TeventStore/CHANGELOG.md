# Changelog

All notable changes to Compze.Teventive.TeventStore will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **A self-generating query model routes its tevents as a taggregate does:** its class, and each of its components' and entities' classes, declares its routing once with `DeclareAppliers` and returns it from its override of `TeventAppliers`. A component's constructor, and `CreateSelfManagingCollection`, take their parent as `SelfGeneratingQueryModel<TQueryModel, TTaggregateTevent>.IParent`, which the query model and its parts are: they're passed `this`.
- Keeps only taggregates whose tevents implement `ITaggregateTeventWithIdentity`, as `Compze.Teventive.TeventStore.Abstractions` now requires.
- **Declares the taggregate classes it keeps,** where it's composed: `TeventStore(connectionName, store => store.Taggregate<User>())`. The container's `ITaggregateStoreUpdater`, `ITaggregateStoreReader` and `ITaggregateStoreBulkReader` route each declared class to it. `ITaggregateStoreBulkReader.ReferencesToAll<TTaggregate>()` gives the histories whose creation tevent is a tevent of the class, and throws when another declared class's tevents are such tevents too, since a history doesn't say which class it is the history of. A class that no store declares fails at its first use, naming the stores and their classes.
- **A deleted taggregate's history is kept:** a taggregate that publishes an `ITaggregateDeletedTevent` has that tevent appended to its history, like any other, and a history that ends with one is no taggregate: `Get`, `TryGet`, `GetForUpdate` and `ITaggregateStoreBulkReader.All` treat it as absent, while `GetHistory` and `GetReadonlyCopyOfVersion` still read the whole history. Before, such a taggregate went on being served, and the updater's delete erased the history. `ITeventStore.DeleteTaggregate` still erases a history, for data that must not be kept. Its id stays used: a new taggregate saved with it fails.
- **A unit of work's taggregates are held for the whole transaction,** not for one scope: two scopes in one transaction get the same instance of a taggregate. The transaction must be begun by `ExecuteUnitOfWork`; getting a taggregate for update in one begun by a `TransactionScope` of its own throws.
- **A publish that fails rolls the unit of work back,** even if the code that published caught the exception.
- **`ITeventStore` is a singleton:** it keeps no state of its own. `TeventStoreForFlexibleTesting` calls its migrations function at each use, so the migrations may change while the container lives.
- `GetReadonlyCopyOfVersion` throws when asked for a version past the taggregate's latest. Before, a version one past the latest returned the latest.
- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.DocumentDb 0.3.0-alpha (was 0.2.0-alpha), Compze.Internals.Logging 0.7.0-alpha (was 0.4.1-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal), Compze.Tessaging 0.9.0-alpha (was 0.5.0-alpha), Compze.Teventive 0.6.0-alpha (was 0.5.0-alpha), Compze.Teventive.TeventStore.Abstractions 0.6.0-alpha (was 0.5.0-alpha), Compze.Threading 0.12.0-alpha (was 0.9.0-alpha), Compze.TypeIdentifiers.DependencyInjection 0.2.0-alpha (was 0.1.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- The tevent store forwards taggregate tevents through the exactly-once publisher's `PublishAsync`, bridging in one deliberate place: the Teventive taggregate model raises tevents synchronously — from constructors and domain methods — and the store forwards them where they are raised, inside the caller's unit of work. The alternative, an async taggregate domain model, would be a redesign of Teventive itself, not a call-site choice.
- Session affinity is transactional, never thread-bound: `TeventStoreUpdater` keeps its `SingleTransactionUsageGuard` and sheds the thread-affinity half (an async unit of work legitimately migrates across threads), while `TeventStore` and the query-model generating reader — which only ever had thread affinity, and are legitimately used by several sequential transactions in one scope — lose their guard rather than gain a constraint they never had.
- `TeventStoreUpdater` publishes the wrapped tevent through `IUnitOfWorkTeventPublisher` (Compze.Abstractions), the one way to publish: publisher identity survives from `Publish` in the taggregate through storage and onward publication, and the tevent's own type decides how far it travels.
- The tevent-refactoring machinery goes internal: `TeventModifier` with its `RefactoredTevent` and `TaggregateTeventWithRefactoringInformation` pair. The migration author's surface is `ITeventModifier`; the class behind it is machinery.
- `TryGet` and `TryGetVersion` on the query-model reader are public, and `TryGetVersion` takes its version before the out parameter — the argument order every other Try-pattern member in the framework uses.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.*` is now `Compze.Teventive.TeventStore.*` — the namespaces this package's name has promised all along.
- The store's currency is now the wrapped tevent: every tevent is persisted exactly as its taggregate published it, inside its publisher's `ITaggregateIdentifyingTevent<TTeventInterface>` wrapper. A row stores the closed wrapper type's `TypeId` and the serialized wrapper object graph, so publisher identity survives storage with zero information loss; hydration deserializes the wrapper and stamps the inner tevent's column-backed properties as before.
- The migration pipeline speaks wrapped tevents end to end: `SingleTaggregateInstanceTeventStreamMutator`, `TeventModifier` (`RefactoredTevent.NewTevent` -> `NewWrappedTevent`, internal stream `Tevents` -> `WrappedTevents`), and `CompleteTeventStoreStreamMutator` all carry `ITaggregateIdentifyingTevent<ITaggregateTevent>`; the `EndOfTaggregateHistoryTeventPlaceHolder` is wrapped like every other tevent in the stream. Migration matching still inspects the inner creation tevent.
- `SelfGeneratingQueryModel` mirrors `ITeventDispatcher`'s two dispatch forms: `ApplyTevent(TTaggregateTevent)` auto-wraps a tevent arriving without a wrapper (such as one delivered to an inner-typed bus subscription), and `ApplyTevent(IPublisherIdentifyingTevent<TTaggregateTevent>)` applies an already-wrapped tevent; `LoadFromHistory` takes the persisted wrapped tevents.
- `SingleTaggregateQueryModelGenerator` feeds its dispatcher the wrapped history; `TeventStoreApi.Tueries.GetHistory<TTevent>` returns `IEnumerable<ITaggregateIdentifyingTevent<TTevent>>`.
- `ITeventStore.StreamTaggregateIdsInCreationOrder`'s tevent-type filter is translated by the routing model's one translation rule: an inner tevent type matches every wrapping of it; a wrapper type matches as it stands.
- `TeventStoreUpdater` publishes the wrapped tevent through `ITeventPublisher` (Compze.Abstractions), the one way to publish: publisher identity survives from `Publish` in the taggregate through storage and onward publication, and the tevent's own type decides how far it travels.

## 0.3.1-alpha

- Updated to stay compatible with Compze.Teventive 0.3.1-alpha: registration goes through `ITeventSubscriber`, and the query model base classes accept an optional `TeventDispatcherConfig`.

## 0.3.0-alpha

- Internal refactoring; updated for the restructured Compze package layout.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
