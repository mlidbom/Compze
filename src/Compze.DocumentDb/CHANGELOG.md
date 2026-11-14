# Changelog

All notable changes to Compze.DocumentDb will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Abstractions 0.4.0-alpha (was 0.3.0-alpha), Compze.DependencyInjection 0.9.0-alpha (was 0.8.0-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.4.0-internal), Compze.Tessaging 0.9.0-alpha (was 0.5.0-alpha), Compze.Threading 0.12.0-alpha (was 0.9.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.0-alpha

- Session affinity is transactional, never thread-bound: `DocumentDbSession` keeps its `SingleTransactionUsageGuard` (one session must never serve two transactions) and sheds the thread-affinity half of its guard combination — an async unit of work legitimately migrates across pool threads, and the session moves with its transaction, which flows across awaits.
- **`DocumentDbRegistrationBuilder` is `EndpointDocumentDbRegistrationBuilder`, and `HandleDocumentType<TDocument>()` takes no registrar.** The endpoint declares the document types it stores; the typermedia registrar it declares them into is the endpoint's own. The registrar-taking form remains for compositions that hold one, and takes the plain `TypermediaHandlerRegistrar`.
- **`NoSuchDocumentException` and `AttemptToSaveAlreadyPersistedValueException` live here now**, in `Compze.DocumentDb.Exceptions`. They fly raw out of `IDocumentDbSession.Get`/`Save`, which makes them this package's public API rather than the tevent store's.
- `DocumentDbSession`, `DocumentKey<TDocument>` and `IDocumentDbSqlLayer` go internal: a consumer talks to `IDocumentDbSession`, never to the session class, its keys, or a sql layer.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.1.0-alpha

- Initial pre-release
