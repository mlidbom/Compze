# Changelog

All notable changes to Compze.Teventive.TeventStore.Typermedia will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **`RegisterTeventStore()` extends `ExactlyOnceEndpointBuilder`**, not the deleted `IEndpointBuilder`: a tevent store belongs to an endpoint whose delivery is exactly-once, and the compiler says so now.
- `RegisterHandlersForTaggregate<TTaggregate, TTevent>` takes the plain `TypermediaHandlerRegistrar`. The store declares through the typermedia surface alone — every store tessage is navigated — and the dependency-injection-supporting registrar wrapper it used to take is gone.
- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.3.0-alpha

- The namespaces caught up with the package rename: `Compze.Tessaging.Teventive.TeventStore.Typermedia` is now `Compze.Teventive.TeventStore.Typermedia` — the namespaces this package's name has promised all along.
- Released against Compze.Teventive 0.4.0-alpha and Compze.Teventive.TeventStore 0.4.0-alpha (the wrapped-tevent store currency).

## 0.2.1-alpha

- No changes. Released to stay compatible with Compze.Teventive 0.3.1-alpha and Compze.Teventive.TeventStore 0.3.1-alpha.

## 0.2.0-alpha

- Initial pre-release
