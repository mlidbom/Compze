# Changelog

All notable changes to Compze.Internals.Sql.PostgreSql will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- Schema creation serializes under the engine's advisory lock (`pg_advisory_lock`, acquired, run, and released on one connection — the lock is session-scoped): several endpoints joining one domain database create their schemas concurrently, from one process or many, and IF-NOT-EXISTS guards are not concurrency-safe DDL.
- `PgSqlDomainDatabase(connectionStringName)`: declares the domain database this endpoint joins — registers the connection pool every sql layer the endpoint registers stores its data through; the sql layers wire their shared infrastructure (the type-id interner) themselves.

## 0.2.1-alpha

- README update.

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.3

- Initial pre-release
