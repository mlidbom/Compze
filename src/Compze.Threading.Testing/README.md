# Compze.Threading.Testing

Observe and deterministically coordinate threads in your tests. Trigger that race condition every single test run. Say goodby to Thread.Sleep + Prayer. `IThreadGate` gives complete control and observability.

## ThreadGate

An `IThreadGate` is a point that threads pass through by calling `AwaitPassThrough()`. The gate can be **open** (threads pass immediately) or **closed** (threads block until released). Either way, every pass-through is counted and observable.

### Creating Gates

```csharp
// Closed gate — threads calling AwaitPassThrough() will block
var myGate = IThreadGate.NewClosed(WaitTimeout.Seconds(5), "myGate");

// Open gate — threads pass through immediately (useful as instrumentation)
var myGate = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "myGate");
```

The timeout is how long awaiting operations will wait before throwing. The name appears in diagnostics and logging.

### Two Mental Models

#### 1. Gate as Barrier

A **closed** gate blocks threads. You control exactly when and how many pass.

```csharp
var gate = IThreadGate.NewClosed(WaitTimeout.Seconds(5), "barrier");

// Threads block at `AwaitPassThrough`
runner.Run(() => { gate.AwaitPassThrough(); DoWork(); });
runner.Run(() => { gate.AwaitPassThrough(); DoWork(); });

// Wait until both threads are queued
gate.AwaitQueueLengthEqualTo(2);

// Release them one at a time
gate.AwaitLetOneThreadPassThrough();   // one runs DoWork
gate.AwaitLetOneThreadPassThrough();   // the second thread runs DoWork

// Or release all at once
gate.Open();
```

`AwaitLetOneThreadPassThrough()` lets deterministically lets exactly one thread pass the gate. The call blocks until that thread has actually passed.

#### 2. Gate as Instrumentation Point

An **open** gate lets threads through immediately but still records every pass-through. This lets you observe when code reaches a specific point without affecting its flow.

```csharp
var beforeWork = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "beforeWork");
var afterWork  = IThreadGate.NewOpen(WaitTimeout.Seconds(5), "afterWork");

runner.Run(() =>
{
   beforeWork.AwaitPassThrough();
   DoSomethingThatMightBlock();
   afterWork.AwaitPassThrough();
});

// Deterministically confirm the thread has entered DoSomethingThatMightBlock
beforeWork.AwaitPassedThroughCountEqualTo(1);

// Prove it hasn't finished yet (bounded negative assertion)
afterWork.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(50)).Must().BeFalse();

// Now trigger whatever should unblock it
TriggerUnblock();

// Confirm it completed
afterWork.AwaitPassedThroughCountEqualTo(1);
```

This is the key pattern for testing blocking behavior deterministically:
- `beforeWork` gate proves the thread **entered** the blocking call
- `afterWork.TryAwait...BeFalse()` proves it's **still blocking**
- After triggering the unblock, `afterWork.AwaitPassedThroughCount...` proves it **completed**

### State Properties

| Property    | Meaning                                             |
|-------------|-----------------------------------------------------|
| `Requested` | Total threads that have called `AwaitPassThrough()` |
| `Queued`    | Threads currently blocked, waiting to pass          |
| `Passed`    | Threads that have passed through                    |
| `IsOpen`    | Whether the gate is currently open                  |

`Requested == Queued + Passed` always holds.

### Awaiting State

You can await any condition of the gate using TryAwait or Await manually:
```csharp
gate.TryAwait(() => gate.Passed > 2, WaitTimeout.Seconds(1)).Must().BeTrue(); //All TryAwait* methods Returns false on failure
gate.Await(() => gate.Passed > 2, WaitTimeout.Seconds(1)); //All Await* methods throw on failure
```

For all the common needs there are predefined convenience methods
```csharp
gate.AwaitQueueLengthEqualTo(5);
gate.AwaitPassedThroughCountEqualTo(3);

gate.TryAwaitQueueLengthEqualTo(5, WaitTimeout.Milliseconds(50)).Must().BeTrue();
gate.TryAwaitPassedThroughCountEqualTo(3, WaitTimeout.Milliseconds(50)).Must().BeTrue();
```

### Post-Pass-Through Actions

Inject behavior that runs immediately after a thread passes through the gate — inside the gate's lock, so it's guaranteed to execute before the next thread passes:

```csharp
// Throw an exception in the handler
gate.ThrowPostPassThrough(new IntentionalException());

// Fail the current transaction (for testing exactly-once delivery)
gate.FailTransactionOnPreparePostPassThrough(new Exception("deliberate failure"));

// Custom action with access to the ThreadSnapshot
gate.SetPostPassThroughAction(snapshot => Log($"Thread passed: {snapshot}"));
```

### ExecuteWithExclusiveLockWhen

Run an action while holding the gate's internal lock, but only when a condition is met. Useful for atomically inspecting and modifying gate state:

```csharp
gate.ExecuteWithExclusiveLockWhen(
   condition: () => gate.Queued == 3,
   action: () => { /* runs with exclusive access to gate state */ }
);
```

## GatedCodeSection

Pairs an entrance gate and an exit gate around a block of code. Lets you control and observe both entry and exit independently.

```csharp
var section = IGatedCodeSection.NewClosed(WaitTimeout.Seconds(5), "mySection");

// Threads enter the section by calling Enter()
runner.Run(() => section.Execute(() => DoWork()));
```

Since both gates start closed, the thread blocks at the entrance. You then control the flow:

```csharp
// Let one thread enter, observe it reach the exit gate
section.LetOneThreadEnterAndReachExit();

// The thread is now inside DoWork(), blocked at the exit gate
section.EntranceGate.Passed.Must().Be(1);
section.ExitGate.Queued.Must().Be(1);

// Let it exit
section.ExitGate.AwaitLetOneThreadPassThrough();
```

Or open both gates for transparent instrumentation:

```csharp
var section = IGatedCodeSection.NewOpen(WaitTimeout.Seconds(5), "observe");

runner.Run(() => section.Execute(() => DoWork()));

// Wait for the thread to enter, then exit
section.EntranceGate.AwaitPassedThroughCountEqualTo(1);
section.ExitGate.AwaitPassedThroughCountEqualTo(1);
```

## TestingTaskRunner

Runs actions on background threads and ensures they all complete successfully within a timeout. Exceptions from any task are rethrown on dispose.

```csharp
using var runner = TestingTaskRunner.WithTimeout(10.Seconds());

runner.Run(() => DoWork());
runner.Run(
   () => DoFirstThing(),
   () => DoSecondThing()
);

// On Dispose: waits for all tasks to complete within timeout,
// throws AggregateException if any task failed or timed out
```

## Putting It All Together

A realistic example testing that a blocking method does not return until explicitly unblocked:

```csharp
[XF] public void does_not_return_until_signal_is_raised()
{
   using var signal = new InterprocessSignal(name, global: true);
   var beforeAwaitingGate = IThreadGate.NewOpen(WaitTimeout.Seconds(5));
   var afterAwaitingGate  = IThreadGate.NewOpen(WaitTimeout.Seconds(5));

   _runner.Run(() =>
   {
      beforeAwaitingGate.AwaitPassThrough();     // instrumentation: "I'm about to block"
      signal.TryAwait(TimeSpan.FromSeconds(2));  // the blocking call under test
      afterAwaitingGate.AwaitPassThrough();      // instrumentation: "I've unblocked"
   });

   // 1. Confirm the thread has entered TryAwait
   beforeAwaitingGate.AwaitPassedThroughCountEqualTo(1);

   // 2. Prove TryAwait is still blocking
   afterAwaitingGate.TryAwaitPassedThroughCountEqualTo(1, WaitTimeout.Milliseconds(50))
                    .Must().BeFalse();

   // 3. Trigger the unblock
   signal.Raise();

   // 4. Confirm TryAwait returned
   afterAwaitingGate.AwaitPassedThroughCountEqualTo(1);
}
```

## Installation

```shell
dotnet add package Compze.Threading.Testing
```

## License

[MIT](https://github.com/mlidbom/Compze/blob/master/LICENSE.txt)
