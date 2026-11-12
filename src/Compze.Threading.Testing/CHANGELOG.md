# Changelog

All notable changes to Compze.Threading.Testing will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/).

## 0.6.0-alpha

- **Every wait has an async counterpart that holds no thread while it waits.** A test that waits synchronously parks its thread for as long as it waits, and in an async test that is a thread-pool thread, one the code under test may need to run on: on a machine with few processors, enough such waits starve the pool. `IThreadGate` gains `TryAwaitAsync`, `ExecuteWithExclusiveLockWhenAsync`, `AwaitLetOneThreadPassThroughAsync`, `AwaitAsync`, `AwaitClosedAsync`, `AwaitQueueLengthEqualToAsync`, `TryAwaitQueueLengthEqualToAsync`, `AwaitPassedThroughCountEqualToAsync` and `TryAwaitPassedThroughCountEqualToAsync`. `IGatedCodeSection` gains `LetOneThreadEnterAndReachExitAsync` and `LetOneThreadPassAsync`. They return plain tasks, so they don't chain the way the synchronous methods do.
- **Async code under test passes through a gate without holding a thread:** `IThreadGateVisitor.AwaitPassThroughAsync`, and `IGatedCodeSection.ExecuteAsync`. Sync and async passes queue at the same gate and pass the same way. The post-pass-through action runs in the caller's execution context, so an ambient transaction that flows across awaits is the caller's there too.
- **`TestingTaskRunner` runs an action on a dedicated thread instead of a thread-pool thread,** since what a test runs there typically blocks, at a gate or in the code under test. It also gains `Run(Func<Task>)` for async functions, which run on the thread pool: before, an async lambda bound to `Run(Action)` and ran as an `async void` the runner could neither await nor see fail.
- **A gate's timeout names the condition it waited for and the thread pool's state,** along with the gate's own, so a timeout caused by a starved pool says so.
- Breaking only for code that implements `IThreadGate`, `IThreadGateVisitor` or `IGatedCodeSection` itself.

## 0.5.0-alpha

- Refactoring the public API
- README updates
- Doc comments on every public type and method

## 0.2.1-alpha

- update to the latest Compze.Threading

## 0.2.0-alpha.1

Refactoring.

## 0.1.0-alpha.1

- Initial pre-release
