# Compze.Threading

Pit of success threading primitives. Impossible to forget to lock. Automatically resolves and diagnoses deadlocks, including both the involved stack traces in the exception.

## The Problem

With the tools built into the BCL:

- Nothing stops you from accessing shared state without locking. 
- Forget once and you have an intermittent bug apt to be very hard to diagnose.
- Deadlocks hang forever bringing production software to a permantent halt.

Usage requires constant vigilance. Getting it disastrously wrong is all too easy.

## Our Solutions

### Automatic Deadlock Resolution with Dual Stack Traces

Every lock acquisition in all the abstractions shown below use a LockTimeout internally when acquiring locks (default 2 minutes). When deadlocks occur the timeout elapses and we capture **both stack traces** in the exception thrown: the thread that was blocked and the thread that was blocking it. No more guessing which thread held the lock. 

> 💡 If the "winning" thread does not promptly (within 10 seconds) release the lock after the deadlock is resolved its stacktrace will not occur in the exception.

### `IThreadShared<T>` Make Forgetting to Lock Impossible.

```csharp
class MyThreadSafeClass
{
    readonly IThreadShared<PrivateImplementation> _inner = IThreadShared.New(new PrivateImplementation());

    void Add(string key, int value) => _inner.Locked(it => it.Add(key, value));
    int Get(string key) => _inner.Locked(it => it.Get(key));

    class PrivateImplementation{/**/}
}
```

> **💡 Note: There is no way to access the PrivateImplementation instance without acquiring the lock. This resolves whole categories of bugs in one fell swoop.**

### `IAwaitableThreadShared<T>` — Condition Waits with Read/Update Semantics

To be able to efficiently wait for shared state to reach a certain condition you need IAwaitableThreadShared instead:

```csharp
class WorkTracker
{
    readonly IAwaitableThreadShared<PrivateImplementation> _inner = IAwaitableThreadShared.New(new PrivateImplementation());

    void Add(WorkItem item) => _inner.Update(it => it.Add(item));

    void Dispose()
    {
        _inner.UpdateWhen(it => it.IsEmpty, it => it.Dispose());
    }

    class PrivateImplementation{/**/}
}
```

> **💡 Note: Always use `Update` if you change state or are at all unsure. Waiting for conditions will never react to updates made inside `Read`**


## API Overview

### Simple Locking — `IThreadShared<T>`

```csharp
class MyService
{
    readonly IThreadShared<MyState> _state = IThreadShared.New(new MyState());

    public int GetValue() => _state.Locked(state => state.Value);
    public void SetValue(int value) => _state.Locked(state => state.Value = value);
}
```

One operation: `Locked`. The shared state is the lambda parameter. No way to misuse it.

### Read/Update/Wait — `IAwaitableThreadShared<T>`

```csharp
class WorkTracker
{
    readonly IAwaitableThreadShared<MyState> _state = IAwaitableThreadShared.WithDefaultTimeouts(new MyState());
    // or: IAwaitableThreadShared.New(new MyState(), LockTimeout.Seconds(5), WaitTimeout.Seconds(30));

    public int GetValue() => _state.Read(state => state.Value);                                  // read lock
    public void SetValue(int value) => _state.Update(state => state.Value = value);              // update lock + pulse waiters
    public int AwaitReady() => _state.ReadWhen(state => state.IsReady, state => state.Value);    // wait for condition, then read
    public void ProcessWhenReady() => _state.UpdateWhen(state => state.HasWork, state => state.Process()); // wait, then update
    public void AwaitDone() => _state.Await(state => state.IsDone);                              // block until condition is true
}
```

### Lower-Level — `IMonitor` and `IAwaitableMonitor`

> **CRITICAL:** The `*When` methods (`TakeReadLockWhen`, `TakeUpdateLockWhen`, `ReadWhen`, `UpdateWhen`, `Await`, etc.) **release the lock while waiting** for a condition, just like `Monitor.Wait`. When the condition is met, the lock is reacquired before the method returns. This means that if you hold the lock and call a waiting method, other threads can acquire the lock during the wait. All `IAwaitableCriticalSection` implementations — including `IAwaitableMonitor` and `IAwaitableMutex` — share this behavior.

#### Wrap all logic in existing public methods inside calls to _monitor to quickly migrate existing classes:
```csharp
class MyThreadSafe
{
    IMonitor _monitor = IMonitor.New();
    
    public void DoStuff() => _monitor.Locked(() => 
    {
        //implementation here
    });
}

class MyAwaitableThreadSafe
{
    IAwaitableMonitor _monitor = IAwaitableMonitor.New();

    public AType ReadAType() => _monitor.Read(() => _aField);
    public void UpdateSomething(AType value) => _monitor.Update(() => _aField = value);
    public AType AwaitValue() => _monitor.ReadWhen(() => _aField != null, () => _aField);

}
```

#### Advanced/rare/risky usages:

```csharp
class RiskyExample
{
    readonly IAwaitableMonitor _monitor = IAwaitableMonitor.WithDefaultTimeout();

    public void ReadSomething()
    {
        ...
        using(_monitor.TakeReadLock()) { /* read */ }
        ...
    }

    public void UpdateSomething()
    {
        ...
        using(_monitor.TakeUpdateLock()) { /* update, notifies waiters */ }
        ...
    }

    public void WaitThenRead()
    {
        ...
        using(_monitor.TakeReadLockWhen(() => ready)) { /* waits for condition, then reads */ }
        ...
    }
}
```

> **💡 WARNING: Think carefully before doing this. Much of the problems with BCL primitives come right back. Profile before you assume that more granular locking is needed for performance. It is far more rare than you may think.** 