# Changelog

All notable changes to Compze.Internals.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.4.0-alpha

- **`TimeAsserter.ExecuteAsync` no longer blocks a thread for as long as it runs.** It took the machine-wide performance-test lock, a system mutex, on a `Task.Run` thread, as only the thread that takes a mutex may release it, and waited there synchronously for the scenario, which stalled while the thread pool was busy. The turn is now Compze.Threading.Interprocess's `IProcessLock`, a file in `Compze` in the temp directory, which `ExecuteAsync` awaits and holds across the scenario's awaits. A performance test waits up to five minutes for its turn instead of two, and one that gives up says which performance test, in which process, holds it. The processes that share a temp directory share the turn: on Windows that makes it per user, where the mutex was per machine.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.SystemCE 0.12.0-internal (was 0.10.0-internal), Compze.Threading 0.14.0-alpha (was 0.13.0-alpha), Compze.Threading.Interprocess 0.8.0-alpha (was 0.7.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.1-alpha

- **Built against Compze.Internals.SystemCE's `TaskCE.RunAsync`, which was `TaskCE.Run`:** the released version calls the old name, which the new Compze.Internals.SystemCE no longer has, and would fail beside it with a `MissingMethodException`. Nothing else changed.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.SystemCE 0.10.0-internal (was 0.8.0-internal), Compze.Threading 0.13.0-alpha (was 0.12.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.3.0-alpha

- **Targets .NET 11 (`net11.0`) instead of .NET 10, and is compiled with runtime async:** the runtime, instead of state machines the compiler generates, suspends and resumes its async methods, so live stack traces show the real call chain. An application that uses this version must target .NET 11.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.Logging 0.7.0-alpha (was 0.6.0-alpha), Compze.Internals.SystemCE 0.8.0-internal (was 0.7.0-internal), Compze.Threading 0.12.0-alpha (was 0.11.0-alpha), Compze.Threading.Interprocess 0.7.0-alpha (was 0.6.0-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.2.0-alpha

- `PollAwaitAsync` and `TryPollAwaitAsync`: polling for a condition in async code, holding no thread between evaluations. A synchronous poll in an async specification sleeps a thread-pool thread between evaluations, a thread the code under specification may need to run on.
- **Requires newer minor versions of other Compze packages:** Compze.Internals.Logging 0.6.0-alpha (was 0.4.1-alpha), Compze.Internals.SystemCE 0.7.0-internal (was 0.4.0-internal), Compze.Threading 0.11.0-alpha (was 0.9.0-alpha), Compze.Threading.Interprocess 0.6.0-alpha (was 0.5.1-alpha). Upgrading to this version upgrades each package listed here too, so their changelogs apply to this upgrade as well.

## 0.1.1-alpha

- The visibility sweep reaches this package: non-public machinery moves below `_internal`/`_private` namespace sections — the markers that replaced the old `Internal`/`Private` spelling — and types and members are narrowed to the least visibility that compiles.

## 0.1.0-alpha

- Initial pre-release
