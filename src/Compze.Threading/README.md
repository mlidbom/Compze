# Compze.Threading

Pit of success threading primitives. Impossible to forget to lock. Automatically resolves and diagnoses deadlocks, including both the involved stack traces in the exception.

## The Problem

With the tools built into the BCL:

- Nothing stops you from accessing shared state without locking. 
- Forget once and you have an intermittent bug apt to be very hard to diagnose.
- Deadlocks hang forever bringing production software to a permanent halt.

Usage requires constant vigilance. Getting it disastrously wrong is all too easy.

## Our Solutions

### Automatic Deadlock Resolution with Dual Stack Traces

Every lock acquisition in all the abstractions shown below use a LockTimeout internally when acquiring locks (default 2 minutes). When deadlocks occur the timeout elapses and we capture **both stack traces** in the exception thrown: the thread that was blocked and the thread that was blocking it. No more guessing which thread held the lock. 

> 💡 If the "winning" thread does not promptly (within 10 seconds) release the lock after the deadlock is resolved its stacktrace will not occur in the exception.

> 💡 The timeout covers taking a lock. A wait for a condition waits forever by default (`WaitTimeout.Default` is `WaitTimeout.Infinite`), since nothing says how long a wait may take in general: a worker waiting for work may rightly wait for hours. Pass a `WaitTimeout`, to the call or when creating the monitor, where a condition that never comes true should fail instead.

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

> **💡 `Read` declares that the code changes nothing, and `Update` that it changes the state.** That tells whoever reads the code which is which, and it lets a read skip waking the waiting conditions: only an update can make a condition true. So a change inside `Read` is forbidden: it wakes no waiting condition, which goes on waiting. Conditions must not change the state either.

> **💡 All locks are exclusive, read locks included.** `Read` declares what the code does, not that it may run alongside other reads.


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
    readonly IAwaitableThreadShared<MyState> _state = IAwaitableThreadShared.New(new MyState());
    // or: IAwaitableThreadShared.New(new MyState(), LockTimeout.Seconds(5), WaitTimeout.Seconds(30));

    // Read under lock. Does NOT wake waiting threads.
    public int GetValue() => _state.Read(state => state.Value);

    // Update under lock and notify all waiting threads to re-evaluate their conditions
    public void SetValue(int value) => _state.Update(state => state.Value = value);  
    

    // Wait for condition to become true, then read while still holding the lock used for checking the condition
    public int AwaitReady() => _state.ReadWhen(state => state.IsReady, state => state.Value);

    // Wait for condition to become true, then update while still holding the lock used for checking the condition, then notify all waiting threads about the update before releasing the lock.
    public void ProcessWhenReady() => _state.UpdateWhen(state => state.HasWork, state => state.Process()); 

    // Wait for condition to become true
    public void AwaitDone() => _state.Await(state => state.IsDone);

    // Each waiting method has an async counterpart that holds no thread while it waits: see "Async condition waits" below
    public async Task AwaitDoneAsync() => await _state.AwaitAsync(state => state.IsDone);
}
```

### Lower-Level — `IMonitor` and `IAwaitableMonitor`

> **CRITICAL:** The `*When` methods (`TakeReadLockWhen`, `TakeUpdateLockWhen`, `ReadWhen`, `UpdateWhen`, `Await`, etc.) **release the lock while waiting** for a condition, just like `Monitor.Wait`. When the condition is met, the lock is reacquired before the method returns. This means that if you hold the lock and call a waiting method, other threads can acquire the lock during the wait. All `IAwaitableCriticalSection` implementations — including `IAwaitableMonitor` and `IAwaitableMutex` — share this behavior.

#### Async condition waits hold no thread while they wait

`TryAwaitAsync`, `ReadWhenAsync`, `UpdateWhenAsync` and `TryUpdateWhenAsync` are the condition waits for async code. While the
condition is false nothing waits but a task, which the next update-lock release completes, so no thread is parked: a synchronous
wait on a thread-pool thread holds that thread for as long as it waits, and enough of them starve the pool.

```csharp
public async Task<AType> AwaitValueAsync() => await _monitor.ReadWhenAsync(() => _aField != null, () => _aField);
```

Taking the lock is still synchronous, and brief: it waits only while another holder is inside a critical section. The condition and
the function a wait runs once the condition holds run under the lock, so they are synchronous. And unlike the synchronous waits, an
async wait can't release a lock its caller already holds, because it may resume on another thread: starting one while holding the
lock fails at once.

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

#### Lock the entire method body with a using statement. For very hot path code, where even a lambda allocation might be worth caring about.
```csharp
class MyThreadSafe
{
    IMonitor _monitor = IMonitor.New();
    
    public void DoStuff()
    {
        using var heldLock = _monitor.TakeLock();
        //implementation here
    }
}

class MyAwaitableThreadSafe
{
    IAwaitableMonitor _monitor = IAwaitableMonitor.New();

    public AType ReadAType()
    {
        using var readLock = _monitor.TakeReadLock();
        return _aField;
    }

    public void UpdateSomething(AType value)
    {
        using var updateLock = _monitor.TakeUpdateLock();
        _aField = value;
    }
    public AType AwaitValue()
    {
        using var readLock = _monitor.TakeReadLockWhen(() => _aField != null);
        return _aField;
    }
}
```
> **💡 Take*Lock operations are zero allocation, the disposables returned are reused**

#### Lock only in chosen sections of code:  Advanced/rare/risky usage:

```csharp
class RiskyExample
{
    readonly IAwaitableMonitor _monitor = IAwaitableMonitor.WithDefaultTimeout();

    public void ReadSomething()
    {
        //Unsynchronized code
        using(_monitor.TakeReadLock()) { /* read */ }
        //Unsynchronized code
    }

    public void UpdateSomething()
    {
        //Unsynchronized code
        using(_monitor.TakeUpdateLock()) { /* update, notifies waiters */ }
        //Unsynchronized code
    }

    public void WaitThenRead()
    {
        //Unsynchronized code
        using(_monitor.TakeReadLockWhen(() => ready)) { /* waits for condition, then reads */ }
        //Unsynchronized code
    }
}
```

> **💡 WARNING: Think carefully before doing this. Much of the problems with BCL primitives come right back. Profile before you assume that more granular locking is needed for performance. It is far more rare than you may think.** 