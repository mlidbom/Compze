# Changelog

All notable changes to Compze.Internals.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.2.0-alpha

- `PollAwaitAsync` and `TryPollAwaitAsync`: polling for a condition in async code, holding no thread between evaluations. A synchronous poll in an async specification sleeps a thread-pool thread between evaluations, a thread the code under specification may need to run on.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.Logging 0.6.0-alpha (was 0.4.1-alpha), Compze.Internals.SystemCE 0.7.0-internal (was 0.4.0-internal), Compze.Threading 0.11.0-alpha (was 0.9.0-alpha), Compze.Threading.Interprocess 0.6.0-alpha (was 0.5.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.1.1-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.1.0-alpha

- Initial pre-release
