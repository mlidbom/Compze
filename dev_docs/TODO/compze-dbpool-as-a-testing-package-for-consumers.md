# Compze.DbPool as a testing package for consumers

Decided by Magnus 2026-11-26: written down for when a consumer's specifications need a server database. Not started.

## Why

Magnus asked tessaging-and-taggregates to have the tooling's specifications get their hubs' SQLite databases from
Compze.DbPool. It doesn't fit a consumer:

- **Only Compze's own tests can use its automatic hook.** It goes through `TestingComponentRegistrar`
  (`src/Compze.Hosting.Testing/Wiring/`), whose SQL layer and serializers come from `TestEnv`. That throws unless
  `TestEnv.XunitDiscoverer` is set, and only Compze's unpublished Compze.Tests.Infrastructure sets it.
- **Compze.Hosting.Testing brings in every DI container,** the database drivers and Newtonsoft.
- **A scenario can't own a pool outside its containers.** `DbPool` is a singleton of the container that registers it, so
  disposing a hub's container disposes the pool in it, which deletes the database the next hub has to read. Nothing
  public makes a pool outside a container.
- **The reservation length is fixed at 65 seconds,** 10 minutes under a debugger: `RegisterWith` takes it, and is
  internal.
- **Every pool on the machine shares 100 databases** (`DbPool.NumberOfDatabases`), a consumer's runs with Compze's own.

What the tooling does instead, for SQLite: no pool. Each scenario makes its database file in a temporary folder of its
own and hands it to the container through `SqliteDomainDatabase`'s overload that takes the file. A pool exists to reuse
costly server databases, and a SQLite database is only a file.

## The change

For when a consumer needs a server database in its specifications:

- A pool a test owns, made outside any container, whose reservations the test hands to each container it builds.
- A testing registrar that is told its SQL layer and serializer, rather than reading them from `TestEnv`, and brings in
  only what it is told to.
- A reservation length the consumer sets.
- No machine-wide reservation for SQLite, whose databases are files that nothing else on the machine competes for.
