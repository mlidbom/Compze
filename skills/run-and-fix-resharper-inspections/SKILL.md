---
name: run-and-fix-resharper-inspections
description: >-
  Run a headless ReSharper inspection pass (jb inspectcode) over a .NET solution and systematically
  correct the findings: parse the SARIF output, scope to one inspection type at a time, fix in
  build-gated batches grouped by file, dodge the known traps (override chains, Newtonsoft silently
  losing data on internal getters, making the wrong overload private), then build + full test suite
  and re-run to confirm zero. Use for whole-solution inspection sweeps and systematic cleanup
  campaigns — "fix all MemberCanBeInternal", "clean up inspection warnings", severities below
  warning, or when no IDE is running. Not for targeted per-file diagnostics — a live
  ReSharper-backed MCP (get_diagnostics / lint_files) is faster when one is available.
---

# Run and fix ReSharper inspections

## Prerequisites

- JetBrains CLI tools (`jb`) must be on PATH
- The solution must build cleanly before running inspections

## The campaign loop

1. **Build** the solution to confirm it's clean
2. **Run** the inspection
3. **Parse** the output, filtered to ONE inspection type
4. **Fix** all reported issues (see "Correcting issues" below)
5. **Build** to catch compile errors from incorrect changes
6. **Test** (full suite) to catch runtime/serialization issues the compiler can't see
7. **Re-run** the inspection to confirm the count dropped to zero (or to the expected suppression count)

**Always work one inspection `ruleId` at a time.** Mixing inspection types in one pass makes it hard to
apply fixes systematically and verify correctness.

## Running an inspection

```powershell
jb inspectcode <path-to-your-solution> `
   --output=inspection-results.sarif `
   --severity=SUGGESTION `
   --include="**/*.cs" `
   --exclude="**/_docs/**"
```

Key flags:
- `--severity` controls the minimum severity: `HINT`, `SUGGESTION`, `WARNING`, `ERROR`
- `--include` / `--exclude` use glob patterns to scope which files are analyzed
- `--output` writes SARIF (JSON) results

Deeper engine detail — flag gotchas (values need `=`, `--no-build`, `--project` prefix matching), XML-format
parsing, the live-MCP fast path, and non-.NET engines — lives in the companion `jetbrains-inspect` skill
(same catalog; linked as `shared-jetbrains-inspect` in consuming repos).

## Parsing the output

```powershell
$sarif = Get-Content inspection-results.sarif -Raw | ConvertFrom-Json
$results = $sarif.runs[0].results

# Filter to a specific inspection type
$filtered = $results | Where-Object { $_.ruleId -eq 'MemberCanBeInternal' }

# Format as a readable list
$filtered | ForEach-Object {
   $loc = $_.locations[0].physicalLocation
   "$($loc.artifactLocation.uri):$($loc.region.startLine) — $($_.message.text)"
}
```

Common `ruleId` values: `MemberCanBeInternal`, `MemberCanBePrivate`, `UnusedMember.Global`, `ClassCanBeSealed.Global`, etc.

## Correcting issues at reported lines

For each reported issue: **read the file at the reported line, understand the context, then edit.**

Do NOT blindly apply changes at line numbers. The report reflects files as they were when the inspection
ran. As you edit files, line numbers shift. Always read the file to find the actual code.

Batch editing strategy:

1. **Group issues by file.** Read each file once, apply all fixes for that file, move on.
2. **Use multi-edit operations** to apply multiple fixes within one file or across files in a single call.
3. **Build after each batch** of files (every ~10-20 files). Catching errors early avoids cascading confusion.
4. **Run the full test suite once** at the end after the build is clean.

## Handling specific inspection types

### MemberCanBeInternal

Change `public` to `internal` on the reported member. Watch for:

- **Override methods**: If you make an `abstract` or `virtual` base method `internal`, **all overrides must also become `internal`**. The compiler enforces this (`CS0507`), so the build will catch it.
- **Serialized properties**: Properties serialized by Newtonsoft (or similar) must have a `public` getter. Newtonsoft only serializes public properties by default. Making the getter `internal` causes silent data loss — the property deserializes as `null`/default. **The compiler will NOT catch this.** Only tests will. Note: private *setters* are fine — Newtonsoft uses reflection to write values regardless of setter accessibility.
- **Solution completeness**: ReSharper analyzes at solution scope. Its analysis is only correct if the solution contains **all** projects that consume the code. Before running inspections, verify that no projects are missing from the solution file.

When a member must stay `public` despite ReSharper's suggestion, add a suppression comment:
```csharp
// ReSharper disable once MemberCanBeInternal — Serialized via Newtonsoft
public TaggregateId TaggregateId { get; set; }
```

### MemberCanBePrivate, ClassCanBeSealed, etc.

Same general approach: read, understand context, apply. The pitfalls above (serialization, cross-assembly,
override chains) apply to all visibility/accessibility inspections.

### MemberCanBePrivate — overloaded methods

When ReSharper reports that one overload can be private, verify which overload it means. A common pattern
is a `public` convenience overload that delegates to a `private` implementation overload. Making the wrong
one private breaks external callers. Always read the file to confirm which overload is the entry point
before changing visibility.

## Verification

1. **Build**: Catches access modifier conflicts (CS0507, CS0117, CS1929, etc.)
2. **Test**: Catches serialization and runtime issues the compiler can't see
3. Both are mandatory. A clean build is necessary but not sufficient.
