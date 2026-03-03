# Compze.Threading

Pit of success threading primitives. Impossible to forget to lock. Automatically resolves and diagnoses deadlocks, including both the involved stack traces in the exception.

## The Problem

With the tools built into the BCL:

- Nothing stops you from accessing shared state without locking. 
- Forget once and you have a bug. 
- Deadlocks hang forever with no diagnostics.

Usage requires constant vigilance. Getting it disastrously wrong is all too easy.

## Our Solutions

### Automatic Deadlock Resolution with Dual Stack Traces

Every lock acquisition in all the abstractions shown below use a timeout internally. When deadlocks occur the timeout elapses and we capture **both stack traces** in the exception thrown: the thread that was blocked and the thread that was blocking it. No more guessing which thread held the lock. If the "winning" thread does not promptly release the lock after the deadlock is resolved its stacktrace will not occur in the exception, but both stack traces will be logged when it does release so the full diagnostic information you need will be there in the logs.

### `IThreadShared<T>` — Make Forgetting to Lock Impossible

```csharp
readonly IThreadShared<Dictionary<string, int>> _cache = IThreadShared.New(new Dictionary<string, int>());

void Add(string key, int value) => _cache.Locked(data => data[key] = value);
int Get(string key) => _cache.Locked(data => data[key]);
```

> **💡 Note: There is no way to access the dictionary without acquiring the lock. This resolves whole categories of bugs in one fell swoop.**

### `IAwaitableThreadShared<T>` — Condition Waits with Read/Update Semantics

When you need to wait for shared state to reach a certain condition you need IAwaitableThreadShared instead:

```csharp
readonly IAwaitableThreadShared<HashSet<WorkItem>> _tasks = IAwaitableThreadShared.New(new HashSet<WorkItem>());

void Add(Task task) => _tasks.Update(it => it.Add(task));

void Dispose()
{
    _tasks.Await(it => !it.Any()); // Block until a condition is met
    ....
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

#### Wrap all logic in all public methods inside calls to _monitor:
```csharp
class MyThreadSafe
{
    IMonitor _monitor = IMonitor.New();
    
    public void DoStuff() => _monitor.Locked(() => 
    {
        //implementation here
    });
}

class AwaitableThreadSafe
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
        using(_monitor.TakeReadLock()) { /* read */ }
    }

    public void UpdateSomething()
    {
        using(_monitor.TakeUpdateLock()) { /* update, notifies waiters */ }
    }

    public void WaitThenRead()
    {
        using(_monitor.TakeReadLockWhen(() => ready)) { /* waits for condition, then reads */ }
    }
}
```

> **💡 Note: Think carefully before doing this. Much of the problems with BCL primitives come right back** 