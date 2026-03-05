# Compze.Utilities.SystemCE.ThreadingCE.Testing

Testing utilities for deterministic multi-threaded tests in [Compze](https://github.com/mlidbom/Compze).

## What's in this package?

Deterministic threading test infrastructure: thread gates, gated code sections, and snapshot utilities for writing reliable multi-threaded tests.

### ThreadGate

A gate that controls thread execution flow for deterministic testing:

```csharp
var gate = ThreadGate.CreateWithTimeout(1.Seconds());

gate.Await();        // Block the current thread at the gate
gate.Open();         // Release all waiting threads
gate.AwaitPassedThrough();  // Wait until a thread has passed through
```

### GatedCodeSection

Wraps a code section with entry/exit gates for fine-grained control:

```csharp
var section = new GatedCodeSection();
section.EntryGate.Open();   // Allow threads to enter
section.ExitGate.Open();    // Allow threads to exit
```

## Installation

```shell
dotnet add package Compze.Utilities.SystemCE.ThreadingCE.Testing
```

## License

[MIT](https://github.com/mlidbom/Compze/blob/master/LICENSE.txt)
