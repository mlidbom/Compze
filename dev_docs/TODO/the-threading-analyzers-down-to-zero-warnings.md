# The threading analyzers, down to zero warnings

Magnus, 2026-11-24: add Microsoft.VisualStudio.Threading.Analyzers everywhere, and land it on main only once the build is
back at zero warnings, so that every session's "C-Build reports 0 warnings" check keeps working meanwhile. Being done on
the branch `threading-analyzers-everywhere`, by state-store-in-tables.

## Why

Among its rules, VSTHRD002 flags a synchronous wait on a task, `.Wait()`, `.Result` or `GetAwaiter().GetResult()`,
wherever it is. No rule of `AnalysisLevel latest-all` does: CA1849 flags one only inside a method that returns a task.
Such a wait needs a free thread-pool thread, and stalls while the pool is busy: the memory image state store's deck flake
of 2026-11-24, and the starvation of 2026-11-12.

## Decided

- **In every project:** `src/Directory.Build.props`, which every project under src, test and samples imports, and the two
  release-check projects under `.github/scripts`. Private assets: no Compze package depends on it.
- **VSTHRD003 off everywhere** (Magnus): it guards against deadlocks with Visual Studio's JoinableTaskFactory, which Compze
  doesn't use.
- **VSTHRD200, the Async suffix, in library code only** (Magnus): off in specifications, whose names say what they
  specify. A public name that deliberately breaks it, such as Compze.Must's `InvokingAsync`, gets a same-line
  suppression with its reason; an override of a framework's method, which can't be renamed, too.

## What remains, 2026-11-24 (a3464550c)

132 warnings from the analyzers, and main's own CA2213 in `DbConnectionPool.TransactionSpecificConnection`, reported to
commit-as-the-decision.

- **VSTHRD200, 39, all in library code:** rename. Compze.Internals.SystemCE 11 (TaskCE), Compze.Tessaging 10,
  Compze.Tessaging.Transport.AspNetCore 8 (the transport controller's actions), Compze.Threading.Testing 3,
  Compze.Tessaging.Hosting.Testing 3, Compze.Must 2, Compze.xUnitBDD 1, Compze.Threading 1.
- **VSTHRD110, 35, an awaitable result not observed:** Compze.Tessaging 3, to read one by one; the rest in the Threading
  specifications, mostly tasks started on purpose and left running, which a discard states.
- **VSTHRD002, 58, the synchronous waits:** in library code 24 (Compze.Tessaging 11, Compze.Internals.SystemCE 3, the DI
  containers' synchronous disposals 4, which `container-disposal-of-async-only-components.md` covers, and others), each
  fixed or, where deliberate, suppressed with its reason; in specifications 34, made asynchronous where they can be.
