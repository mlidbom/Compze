# Changelog

All notable changes to Compze.Typermedia will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- The navigators speak Local/Remote and unit-of-work language. `IInProcessTypermediaNavigator` is `IUnitOfWorkLocalTypermediaNavigator`: the honest axis for navigation is whose API you browse (this endpoint's own = local, another endpoint's = remote), and execution joins the caller's unit of work — tueries need no transaction but join the caller's consistency, tommands demand the caller's ambient transaction. New `IIndependentLocalTypermediaNavigator` for code outside any unit of work: each tommand as its own unit of work, each tuery in its own transactionless scope, independence asserted. `IRemoteTypermediaNavigator` is Singleton-only and its dual-registrar special case (`SingletonRemoteTypermediaNavigator`) is deleted: remote navigation has no unit-of-work relationship — a typermedia tessage cannot be sent remotely from within a transaction — so its Scoped registration was a false duality. Tommand handlers registered through `ITypermediaHandlerRegistrar` now receive `IUnitOfWorkResolver`; tuery handlers keep `IScopeResolver`, deliberately.

- `AddInProcessTypermedia()` / `InProcessTypermediaEndpointFeature`: strictly-local Typermedia as a composable endpoint feature — the typermedia handler registry and the in-process navigator, no transport — mirroring in-process Tessaging. `RegisterTypermediaHandlers` on the endpoint builder now composes it; distributed Typermedia composes it and adds the transport-speaking client side.
- The at-most-once typermedia tommand signatures speak the renamed tessage type: `IAtMostOnceTypermediaTommand<TResult>` (formerly `IAtMostOnceTommand<TResult>`) throughout `IRemoteTypermediaNavigator`, `ITypermediaRouting`, and `NavigationSpecification`.

## 0.1.1-alpha

- Fixed packaging: the `_docs` markdown files were packed into the NuGet package as contentFiles, so installing the package injected linked `_docs\*.md` items into consuming projects. They no longer ship in the package.

## 0.1.0-alpha

- Initial pre-release
