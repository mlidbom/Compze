# Changelog

All notable changes to Compze.Internals.Sql.Sqlite will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- `SqliteEndpointDatabase`: the declaration that an endpoint's database is sqlite. `SqliteEndpointDatabase(connectionStringName)` registers the endpoint's connection pool, and the declaration type carries the connection-string name the sqlite pairings derive their wiring from — e.g. the type-id interner's own database name, which on sqlite lives in a separate database file.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
