# Eliminating the Public API Surface of SystemCE

## Context

`Compze.Utilities.SystemCE` contains ~65 low-level utility types — extension methods, helper classes, and small abstractions (`TimeSpanCE`, `StringCE`, `LazyCE`, `EnumCE`, `CastCE`, etc.) used pervasively across the solution. These were all **public**, which meant:

- Every consumer took a dependency on the concrete public API of SystemCE
- Any refactoring of a utility type risked breaking downstream packages
- The NuGet package exposed implementation details that aren't part of the framework's intentional API

The goal: make these types **internal to each consuming assembly** so they become invisible implementation details, while keeping the code in a single maintainable location.

## Alternatives Considered

### 1. Keep everything public (status quo)

**How it works:** All SystemCE types remain public. Every project references the SystemCE package normally.

**Pros:**
- Zero effort — nothing to change
- Standard .NET dependency model
- IDE navigation "just works"

**Cons:**
- Wide public API surface that leaks implementation details
- Fragile versioning — changing `TimeSpanCE.Seconds()` signature is a breaking change for all consumers
- Couples unrelated packages through shared public utility types
- Consumers see types in IntelliSense that aren't part of the framework's intentional API

**Verdict:** Rejected — the public surface problem was the motivation for this work.

### 2. InternalsVisibleTo

**How it works:** Keep types internal in SystemCE, grant access to specific assemblies via `[InternalsVisibleTo]`.

**Pros:**
- Types become internal (invisible to external consumers)
- Single compiled assembly — no code duplication

**Cons:**
- Still one copy of the code — all assemblies share the same type identity, which means the SystemCE package is still a runtime dependency
- Requires maintaining an explicit allow-list of friend assemblies
- Doesn't work across NuGet package boundaries (the consuming package would need to be listed at publish time)
- Doesn't solve the core problem: utility types are still in a separate assembly that must be deployed and versioned

**Verdict:** Rejected — doesn't eliminate the package dependency or the versioning coupling.

### 3. InternalizedSourceReferences (Roslyn source generator)

**How it works:** A custom build tool (`Compze.Build.InternalizedSourceReferences`) reads source files from SystemCE at build time, rewrites all `public` declarations to `internal`, and injects the transformed source into the consuming project. ThreadingCE was already using this approach.

**Pros:**
- Types compile as internal — invisible to consumers
- Automatic transformation, no manual maintenance of internal copies

**Cons:**
- Complex build infrastructure — a custom Roslyn-based NuGet package that must be maintained
- Opaque transformation — hard to debug when something goes wrong
- Build-time processing adds complexity to the build pipeline
- IDE support is imperfect — transformed files don't always show up correctly for navigation/debugging
- Yet another moving part in an already complex build system

**Verdict:** This was the existing solution for ThreadingCE. It works but adds unnecessary complexity for what is fundamentally a simple need.

### 4. Source generators (Roslyn incremental generators)

**How it works:** A proper Roslyn incremental source generator that emits the SystemCE types into each consuming compilation.

**Pros:**
- First-class tooling support in modern .NET
- Incremental — only regenerates when inputs change

**Cons:**
- Even more heavyweight than InternalizedSourceReferences for this use case
- Generators are designed for *generating* code from metadata, not for *copying* existing code
- Debugging generator-emitted code is painful
- Adds a generator dependency to every consuming project

**Verdict:** Rejected — wrong tool for the job. We're not generating code, we're sharing existing code.

### 5. Shared source via MSBuild Compile Include (chosen approach)

**How it works:** Source files live in `src/Compze.Shared.Source/SystemCE/`. Each consuming project adds one line to its `.csproj`:

```xml
<Compile Include="..\Compze.Shared.Source\SystemCE\**\*.cs" LinkBase="SharedSource\SystemCE" />
```

The compiler treats these as regular source files. They're written with `internal` visibility in the shared location.

**Pros:**
- **Zero magic** — standard MSBuild, no custom tooling, no generators, no build-time transformation
- **IDE-friendly** — files appear in Solution Explorer under a `SharedSource` virtual folder, full navigation and debugging support
- **Debuggable** — breakpoints work, stack traces point to the real source files
- **Each assembly gets its own internal copy** — no cross-assembly coupling, no versioning fragility
- **Well-established pattern** — used by Microsoft for `System.Runtime.CompilerServices.Unsafe` and many other infrastructure libraries
- **Replaces InternalizedSourceReferences** — eliminates a complex build dependency

**Cons:**
- Each assembly compiles its own copy — slightly increases total compilation time and binary sizes
- Types with **mutable global static state** (like `UncatchableExceptionsGatherer`) cannot be shared this way — each assembly would get an isolated copy of the static fields, breaking cross-assembly coordination
- Adding a new shared file requires no per-project changes (glob pattern catches it), but teams must understand that shared source types are internal and won't be visible across assembly boundaries

**Verdict:** Chosen. The simplest approach that fully solves the problem. The static state limitation affects exactly one type (`UncatchableExceptionsGatherer`), which stays public in SystemCE.

## What We Implemented

- Created `src/Compze.Shared.Source/SystemCE/` containing all 65 SystemCE source files, all marked `internal`
- Created a validation-only `Compze.Shared.Source.csproj` to catch compilation errors in the shared source itself
- Wired ~30 projects to include shared source via `<Compile Include>` globs
- Removed direct SystemCE FlexRef references from all projects (except `Testing.DbPool` which needs `UncatchableExceptionsGatherer`)
- Left `UncatchableExceptionsGatherer` as the sole public type in `Compze.Utilities.SystemCE` due to its global static state requirement
- Removed ThreadingCE's dependency on `InternalizedSourceReferences` for SystemCE types
- Fixed two public API signatures that exposed `LazyCE<T>` — changed to accept `Func<T>` and construct `LazyCE` internally
- All 1164 tests pass, both `Compze.slnx` and `Compze.Samples.slnx` build clean
