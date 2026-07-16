# Changelog

All notable changes to Compze.Teventive.TeventStore.Abstractions will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.*` is now `Compze.Teventive.TeventStore.Abstractions.*` — the namespaces this package's name has promised all along.
- The store abstractions now speak wrapped tevents: `ITeventStore` (histories, `SaveSingleTaggregateTevents`, `StreamTevents`), `ITeventStoreReader.GetHistory`, and `ITeventStoreSerializer` all deal in `ITaggregateIdentifyingTevent<ITaggregateTevent>` - the tevent exactly as its taggregate published it, publisher identity included.
- The migration API speaks wrapped tevents: `ISingleTaggregateInstanceHandlingTeventMigrator.MigrateTevent` receives the full persisted wrapped tevent, and `ITeventModifier.Replace`/`InsertBefore` take complete wrapped replacements - the migration author supplies the wrapper, so publisher identity is rewritten as deliberately as the tevent itself.
- `ITeventStoreTeventPublisher` is removed: the tevent store forwards its committed tevents through `IUnitOfWorkTeventPublisher` (Compze.Abstractions) — publishing's one public surface — like any other client, so the store abstractions no longer own a publishing seam.
- Completed the Aggregate -> Taggregate rename in the save-guard exceptions: `AttemptToSaveAlreadyPersistedAggregateException` -> `AttemptToSaveAlreadyPersistedTaggregateException`, `AttemptToSaveEmptyAggregateException` -> `AttemptToSaveEmptyTaggregateException`.

## 0.3.1-alpha

- No changes. Released to stay compatible with Compze.Teventive 0.3.1-alpha.

## 0.3.0-alpha

- Initial release of extracted project
