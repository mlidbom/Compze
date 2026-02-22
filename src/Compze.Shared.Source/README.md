# Compze.Shared.Source — Shared Internal Source for SystemCE Types

## Problem

The `Compze.Utilities.SystemCE` package provides low-level extension methods and utility types (`TimeSpanCE`, `StringCE`, `LazyCE`, `EnumCE`, etc.) used by virtually every project in the solution. These types were **public**, creating a wide public API surface that:

- Leaked implementation details into the package's public contract
- Made versioning fragile — any change to a utility type was a breaking change for all consumers
- Created coupling between unrelated packages through shared public types

Meanwhile, `Compze.Utilities.SystemCE.ThreadingCE` already solved this for itself using the `InternalizedSourceReferences` build tool — a Roslyn source generator that copies SystemCE source files and rewrites them as `internal`. This worked but was complex build machinery for what is conceptually a simple need: "compile these files as internal in my assembly."

## Solution

Replace the `InternalizedSourceReferences` mechanism and the public SystemCE API surface with **shared source inclusion** — a standard MSBuild pattern where source files live in one location and are compiled directly into each consuming assembly as `internal` types.

### Architecture

```
src/Compze.Shared.Source/SystemCE/     ← 65 source files, all internal
    ├── ActionCE.cs
    ├── StringCE.cs
    ├── LazyCE.cs
    ├── TimeSpanCE.cs
    ├── EnumCE.cs
    ├── LinqCE/
    ├── TransactionsCE/
    ├── ReactiveCE/
    └── ...

src/Compze.Shared.Source/Compze.Shared.Source.csproj   ← validation-only project
```

Each consuming project includes the shared source via a single MSBuild line:

```xml
<Compile Include="..\Compze.Shared.Source\SystemCE\**\*.cs" LinkBase="SharedSource\SystemCE" />
```

The compiler treats these as regular source files in the consuming project — they compile as `internal`, invisible to external consumers, with zero runtime overhead and no cross-assembly coupling.

### What stays public

`UncatchableExceptionsGatherer` remains as the sole public type in `Compze.Utilities.SystemCE`. It uses **mutable static state** (a global list of exceptions registered from finalizers) that must be shared across all assemblies in a process. Making it internal per-assembly would give each assembly its own isolated exception list, breaking the cross-assembly coordination the type exists to provide.

### The validation project

`Compze.Shared.Source.csproj` exists solely to verify the shared source compiles. It references the same dependencies the shared source needs (`Contracts`, `Functional`, `ThreadingCE`) and is included in the solution but produces no distributable output. If someone breaks a shared source file, this project fails immediately rather than producing confusing errors across 30+ consuming projects.

## Why shared source over alternatives

| Approach | Drawback |
|----------|----------|
| **Public types in a package** | Leaks implementation details, fragile versioning |
| **InternalsVisibleTo** | Still one copy, doesn't solve the public API problem |
| **InternalizedSourceReferences** (Roslyn generator) | Complex build machinery, opaque transformation, hard to debug |
| **Source generators** | Heavyweight for simple file inclusion |
| **Shared source (this approach)** | Standard MSBuild, zero magic, IDE-friendly, debuggable |

The shared source pattern is the same approach used by Microsoft's `System.Runtime.CompilerServices.Unsafe` and many other infrastructure libraries. It's the simplest mechanism that achieves the goal.

## Impact

- **~30 projects** now include shared source instead of referencing SystemCE's public types
- **SystemCE package** reduced from 65+ public types to 1 (`UncatchableExceptionsGatherer`)
- **ThreadingCE** no longer uses the `InternalizedSourceReferences` build tool for SystemCE types
- **No runtime behavior change** — the same code runs in the same places, just compiled `internal` per assembly instead of resolved as `public` across assemblies
