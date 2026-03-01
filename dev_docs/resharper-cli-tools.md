# ReSharper Command Line Tools

## Installation

Installed globally as a .NET tool:

```powershell
dotnet tool install -g JetBrains.ReSharper.GlobalTools
```

This provides the `jb` command with two sub-tools: `jb inspectcode` and `jb cleanupcode`.

- **NuGet package**: https://www.nuget.org/packages/JetBrains.ReSharper.GlobalTools
- **Same binaries** as the standalone download at https://www.jetbrains.com/resharper/download/#section=commandline — the NuGet approach is just the modern distribution method.

## Tools

### InspectCode — Find Code Issues

Runs ReSharper's code inspections without opening the IDE. Produces a report of all detected issues.

```powershell
# Full solution (lets it build first — recommended)
jb inspectcode src/Compze.slnx --output=inspect-report.xml --format=Xml --severity=WARNING

# SARIF format (default, integrates with VS Code SARIF Viewer and GitHub code scanning)
jb inspectcode src/Compze.slnx --output=inspect-report.sarif --severity=WARNING

# Specific project only
jb inspectcode src/Compze.slnx --project=Compze.Core --output=core-report.sarif

# Include suggestions (more verbose)
jb inspectcode src/Compze.slnx --severity=SUGGESTION

# Multiple output formats in one run
jb inspectcode src/Compze.slnx --format=Html;Xml --output=reports/
```

**Key parameters:**

| Parameter | Description |
|---|---|
| `--output (-o)` | Output file path. Use `-` for stdout. |
| `--format (-f)` | `Sarif` (default), `Xml`, `Html`, `Text`. Combine with `;`. |
| `--severity (-e)` | Minimum severity: `INFO`, `HINT`, `SUGGESTION` (default), `WARNING`, `ERROR` |
| `--project` | Wildcard to limit to specific projects, e.g. `--project=*Core` |
| `--include/--exclude` | File masks using Ant-style wildcards (`**/*.cs`, etc.) |
| `--swea/--no-swea` | Enable/disable solution-wide analysis |
| `--no-build` | Skip building (can cause I/O errors — generally let it build) |
| `--settings (-s)` | Path to a custom `.DotSettings` file |
| `--dumpIssuesTypes (-it)` | List all available inspection IDs (run without solution arg) |
| `--jobs (-j)` | Parallel jobs. 0 = auto (default) |

### CleanupCode — Auto-Fix Code Style

Applies code cleanup (formatting, syntax style, redundancy removal) across a solution. **Modifies files in place** — commit first!

```powershell
# Full cleanup using default profile
jb cleanupcode src/Compze.slnx

# Reformat only (no semantic changes)
jb cleanupcode src/Compze.slnx --profile="Built-in: Reformat Code"

# Reformat + syntax style
jb cleanupcode src/Compze.slnx --profile="Built-in: Reformat & Apply Syntax Style"

# Use the custom profile from DotSettings
jb cleanupcode src/Compze.slnx --profile="ManpowerSilentCleanup"

# Only specific files/paths
jb cleanupcode src/Compze.slnx --include="src/Compze.Core/**/*.cs"

# Clean up individual files without a solution (reformat only)
jb cleanupcode src/Compze.Core/SomeFile.cs
```

**Key parameters:**

| Parameter | Description |
|---|---|
| `--profile (-p)` | Cleanup profile name. Default: `Built-in: Full Cleanup` |
| `--include/--exclude` | File masks (Ant-style wildcards, `;`-separated) |
| `--settings (-s)` | Path to a custom `.DotSettings` file |

**Built-in profiles:**
- `Built-in: Full Cleanup` — all cleanup tasks except file header updates
- `Built-in: Reformat Code` — only formatting
- `Built-in: Reformat & Apply Syntax Style` — formatting + syntax style rules

Custom profiles can be defined in `.DotSettings` files (we have `ManpowerSilentCleanup` in `MySettings.DotSettings`).

## Configuration

Both tools automatically pick up settings from:
1. **`.DotSettings` files** — `Compze.slnx.DotSettings` (solution team-shared layer) is found automatically
2. **`.editorconfig` files** — EditorConfig properties override DotSettings where they overlap
3. **Command-line `--settings` parameter** — overrides everything

## Inspection Results (March 2026 Baseline)

Run against `src/Compze.slnx` at WARNING severity: **1,398 issues across 36 issue types**.

### Issues by Category

| TypeId | Count | Description |
|---|---|---|
| `MemberCanBeInternal` | 703 | Public members only used within their assembly |
| `UnusedMember.Global` | 204 | Public members not referenced anywhere |
| `MemberCanBePrivate.Global` | 159 | Members that could be private |
| `UnusedMethodReturnValue.Global` | 58 | Return values nobody reads |
| `UnnecessaryWhitespace` | 51 | Whitespace formatting issues |
| `PossibleInterfaceMemberAmbiguity` | 44 | Interface member resolution ambiguity |
| `UnusedParameter.Local` | 35 | Unused parameters |
| `ClassNeverInstantiated.Global` | 22 | Classes nothing creates |
| `MemberCanBeProtected.Global` | 14 | Public members only used by derived types |
| `RedundantBaseConstructorCall` | 14 | Unnecessary `: base()` calls |
| `RedundantStringInterpolation` | 8 | `$"literal"` where `"literal"` would do |
| `CheckNamespace` | 8 | Namespace doesn't match folder structure |
| Others | 77 | Various smaller categories |

### Issues by Project (Top 10)

| Project | Count |
|---|---|
| Compze.Core | 169 |
| Compze.Tessaging | 154 |
| Compze.Utilities.SystemCE | 132 |
| Compze.Tests.Unit | 106 |
| Compze.Utilities.Testing.Must | 97 |
| Compze.Sql.Sqlite | 59 |
| Compze.Sql.PostgreSql | 56 |
| Compze.Sql.MySql | 53 |
| Compze.Tessaging.Teventive.TeventStore | 47 |
| Compze.Sql.MicrosoftSql | 45 |

### Visibility Reduction Opportunity

876 issues (63% of total) are about visibility that could be tightened:
- 703 `MemberCanBeInternal`
- 159 `MemberCanBePrivate.Global`
- 14 `MemberCanBeProtected.Global`

**Caveat:** InspectCode analyzes within the specified solution only. Members that look unused/over-exposed might be consumed by other solutions, NuGet package consumers, or reflection. Apply visibility changes carefully for types that are part of the public package API.

## Practical Workflow

1. **Commit** your current work
2. **Inspect**: `jb inspectcode src/Compze.slnx --output=inspect-report.xml --format=Xml --severity=WARNING`
3. **Analyze** the report — filter for specific TypeIds you want to address
4. **Fix** either manually or with CleanupCode for style issues
5. **Review** changes with `git diff`
6. **Test**: `dotnet test src/Compze.slnx --no-build`
7. **Commit** the cleanup

For visibility reduction specifically, InspectCode identifies the targets but CleanupCode doesn't auto-fix them — those changes need to be made manually or with IDE refactoring.

## Analyzing XML Reports with PowerShell

```powershell
# Load and summarize
[xml]$report = Get-Content inspect-report.xml
$allIssues = $report.Report.Issues.Project | ForEach-Object { $_.Issue }
"Total issues: $($allIssues.Count)"

# Group by type
$allIssues | Group-Object TypeId | Sort-Object Count -Descending | Format-Table @{L='TypeId';E={$_.Name}}, Count -AutoSize

# Filter for specific issue type
$allIssues | Where-Object TypeId -eq 'MemberCanBeInternal' | Select-Object File, Line, Message | Format-Table -AutoSize

# Group by project
$report.Report.Issues.Project | ForEach-Object {
    [PSCustomObject]@{ Project = $_.Name; Count = ($_.Issue | Measure-Object).Count }
} | Sort-Object Count -Descending | Format-Table -AutoSize
```

## Important Notes

- **`--no-build` can fail** with I/O errors. Let the tools build even if the solution was recently built — MSBuild is fast for no-op builds.
- **CleanupCode modifies files in place**. Always commit before running it.
- **Excluding files doesn't improve performance** — the tool still analyzes everything; exclusions are applied only to the output/changes.
- **Report files should be gitignored** — they're large and machine-specific.

## Official Documentation

- [ReSharper Command Line Tools (overview)](https://www.jetbrains.com/help/resharper/ReSharper_Command_Line_Tools.html)
- [InspectCode Command-Line Tool](https://www.jetbrains.com/help/resharper/InspectCode.html)
- [CleanupCode Command-Line Tool](https://www.jetbrains.com/help/resharper/CleanupCode.html)
- [Code Inspection Index (all inspection IDs)](https://www.jetbrains.com/help/resharper/Reference_Code_Inspection_Index.html)
- [EditorConfig Properties Index](https://www.jetbrains.com/help/resharper/EditorConfig_Index.html)
- [Sharing Configuration Options (DotSettings layers)](https://www.jetbrains.com/help/resharper/Sharing_Configuration_Options.html)
- [Code Cleanup Profiles](https://www.jetbrains.com/help/resharper/Code_Cleanup__Index.html#profiles)
