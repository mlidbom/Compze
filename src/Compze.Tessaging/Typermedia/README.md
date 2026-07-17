> **The `Compze.Typermedia` package folded into `Compze.Tessaging` on 2026-07-17** - this README describes it as
> it was packaged separately; the prose is rewritten when the paradigm's docs are.

# Compze.Typermedia

Core Typermedia infrastructure: handler registry, handler dispatch, and request handling for type-routed request-response APIs.

This package is also the whole of **in-process Typermedia**: `LocalTessagingEngine(...)` composes the handler
registry and the `ILocalTypermediaNavigatorSession` — through which strictly local tueries and tommands
execute synchronously, in the caller's session (a tommand within the caller's transaction) — into a plain
container, with no transport server, no discovery, and nothing to host. Distributed Typermedia (serving remote clients) lives in `Compze.Typermedia.Client` and
composes this package's in-process core.
