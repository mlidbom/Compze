# Changelog

All notable changes to Compze.Teventive.TeventStore.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.8.0-alpha

- **The tevent store's SQL layer reads a taggregate's history for update asynchronously too:** a member of the internal interface that the SQL layer packages implement. Compze.Teventive.TeventStore.MicrosoftSql, .MySql, .PostgreSql and .Sqlite of the versions released with this one implement it, and those released before fail to load beside this version: upgrade them with it.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.5.0-alpha (was 0.4.0-alpha), Compze.Internals.SystemCE 0.12.0-internal (was 0.9.0-internal), Compze.Teventive 0.9.0-alpha (was 0.7.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.7.0-alpha

- **`ITeventStoreUpdater` is gone: Compze.Teventive's `ITaggregateStoreUpdater` replaces it,** one updater for every taggregate store of a container. `Get` is `GetForUpdate`, `TryGet` is `TryGetForUpdate`, `Save` is `Add`, without a type parameter, and `Delete(id)` is gone: a taggregate is deleted by publishing an `ITaggregateDeletedTevent`, and the tevent store keeps its history. It isn't `IDisposable`: the unit of work's scope owns it.
- **`ITeventStoreReader.GetReadonlyCopy` is gone: Compze.Teventive's `ITaggregateStoreReader.Get` replaces it,** the one reader of every store's taggregates. Outside a unit of work, a taggregate of the tevent store comes back built from its history, read only: publishing on it throws. So does publishing on what `GetReadonlyCopyOfVersion` returns. `ITeventStoreReader` keeps only what the tevent store adds, `GetHistory` and `GetReadonlyCopyOfVersion`, which are as they were: code that also reads taggregates takes an `ITaggregateStoreReader` beside it.
- **A tevent store keeps only taggregates whose tevents have identity:** `ITeventStoreReader.GetReadonlyCopyOfVersion` constrains its taggregate to `ITaggregate<ITaggregateTeventWithIdentity>`, as does the declaration of the classes a tevent store keeps, so a taggregate whose tevents have none doesn't compile against them. Histories, migrations and the stream mutators are typed `ITaggregateTevent<ITaggregateTeventWithIdentity>`.
- **The failures of `Add` say add, not save,** as the method is now named: adding a taggregate the tevent store already has, or one with no history to persist. Nothing public changes.
- **Requires newer minor versions of other Compze packages:** Compze.DependencyInjection 0.10.0-alpha (was 0.9.0-alpha), Compze.Internals.SystemCE 0.9.0-internal (was 0.8.0-internal), Compze.Teventive 0.7.0-alpha (was 0.6.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.6.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal), Compze.Teventive 0.6.0-alpha (was 0.5.0-alpha), Compze.TypeIdentifiers 0.3.0-alpha (was 0.2.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.5.0-alpha

- `ITeventStoreTeventPublisher` is removed: the tevent store forwards its committed tevents through `IUnitOfWorkTeventPublisher` (Compze.Abstractions) — publishing's one public surface — like any other client, so the store abstractions no longer own a publishing seam.
- **The store's exception family goes internal.** An exception type is public only when a consumer can sanely catch it and recover: `AttemptToAddAlreadyPersistedTaggregateException`, `AttemptToAddEmptyTaggregateException`, `InvalidHistoryException`, `TaggregateNotFoundException` and `TeventDuplicateKeyException` all report a bug or an infrastructure fault, so none of them is API. `NoSuchDocumentException` moves to `Compze.DocumentDb.Exceptions`, where the session that throws it lives.
- **The sql layer and the storage-row family go internal**: `ITeventStoreSqlLayer`, `TeventDataRow`, `CreationTeventRow`, `TeventInsertionSpecification`, `TeventNeighborhood`, `VersionSpecification`, `TaggregateTeventData`, `ReadOrder`, `TeventTableSchemaStrings`, the two stream mutators behind the public migration seam, and `ITeventStoreSerializer`. No consumer of the library ever touches a sql layer; the backends reach them through `InternalsVisibleTo` like every other backend seam.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.4.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.*` is now `Compze.Teventive.TeventStore.Abstractions.*` — the namespaces this package's name has promised all along.
- The store abstractions now speak wrapped tevents: `ITeventStore` (histories, `SaveSingleTaggregateTevents`, `StreamTevents`), `ITeventStoreReader.GetHistory`, and `ITeventStoreSerializer` all deal in `ITaggregateIdentifyingTevent<ITaggregateTevent>` - the tevent exactly as its taggregate published it, publisher identity included.
- The migration API speaks wrapped tevents: `ISingleTaggregateInstanceHandlingTeventMigrator.MigrateTevent` receives the full persisted wrapped tevent, and `ITeventModifier.Replace`/`InsertBefore` take complete wrapped replacements - the migration author supplies the wrapper, so publisher identity is rewritten as deliberately as the tevent itself.
- `ITeventStoreTeventPublisher` is removed: the tevent store forwards its committed tevents through `ITeventPublisher` (Compze.Abstractions) — publishing's one public surface — like any other client, so the store abstractions no longer own a publishing seam.
- Completed the Aggregate -> Taggregate rename in the save-guard exceptions: `AttemptToSaveAlreadyPersistedAggregateException` -> `AttemptToAddAlreadyPersistedTaggregateException`, `AttemptToSaveEmptyAggregateException` -> `AttemptToAddEmptyTaggregateException`.

## 0.3.1-alpha

- No changes. Released to stay compatible with Compze.Teventive 0.3.1-alpha.

## 0.3.0-alpha

- Initial release of extracted project
