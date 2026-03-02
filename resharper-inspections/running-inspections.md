# Running ReSharper Inspections

## Prerequisites

- JetBrains CLI tools (`jb`) must be on PATH
- The solution must build cleanly before running inspections

## Running an Inspection

```powershell
jb inspectcode src/Compze.AllProjects.slnx `
   --output=inspection-results.sarif `
   --severity=SUGGESTION `
   --include="**/*.cs" `
   --exclude="**/_docs/**"
```

Key flags:
- `--severity` controls the minimum severity: `HINT`, `SUGGESTION`, `WARNING`, `ERROR`
- `--include` / `--exclude` use glob patterns to scope which files are analyzed
- `--output` writes SARIF (JSON) results

## Parsing the Output

The output is SARIF (JSON) format. Parse with PowerShell to get a workable list:

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

## Workflow

1. **Build** the solution to confirm it's clean
2. **Run** the inspection for a specific `TypeId` you want to fix
3. **Parse** the output to get the file + line + member list
4. **Fix** all reported issues (see [correcting-reported-lines.md](correcting-reported-lines.md))
5. **Build** to catch compile errors from incorrect changes
6. **Test** to catch runtime/serialization issues the compiler can't see
7. **Re-run** the inspection to confirm the count dropped to zero (or to the expected suppression count)

## Scoping to a Single Inspection Type

Always filter to one `TypeId` at a time. Mixing inspection types in one pass makes it hard to apply fixes systematically and verify correctness.
