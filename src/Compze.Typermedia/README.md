# Compze.Typermedia

Core Typermedia infrastructure: handler registry, handler dispatch, and request handling for type-routed request-response APIs.

This package is also the whole of **in-process Typermedia**: `InProcessTypermedia()` composes the handler
registry and the `IUnitOfWorkLocalTypermediaNavigator` — through which strictly local tueries and tommands execute
synchronously, in the caller's transaction — into a plain container, with no transport server, no discovery,
and nothing to host. Distributed Typermedia (serving remote clients) lives in `Compze.Typermedia.Client` and
composes this package's in-process core.
