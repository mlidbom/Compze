# NCrunch Console Tool — Findings

## Installation

- Console tool: `C:\Program Files (x86)\Remco Software\NCrunch Console Tool\NCrunch.exe`
- Requires a license: `NCrunch.exe /License` (same license as the VS plugin, no extra purchase needed)
- `NCrunchCacheStoragePath` must be configured (already set to `c:\NCrunch\cache` in global config)

## The `.slnx` Problem

**The NCrunch console tool does NOT support `.slnx` files.** The help text says it accepts ".sln or .proj" targets. When given a `.slnx`, it treats it as a `.csproj`-style project file and injects MSBuild `<Target>` elements into `<Project>` nodes, corrupting the XML.

This is invisible in the VS plugin because Visual Studio handles `.slnx` parsing — the console tool does its own.

### Workaround: Generate a `.sln`

```powershell
cd src
dotnet new sln -n CompzeForNCrunch -o . --format sln
dotnet sln CompzeForNCrunch.sln add <all projects from slnx>
Copy-Item Compze.v3.ncrunchsolution CompzeForNCrunch.v3.ncrunchsolution
```

The `.ncrunchsolution` copy is required so NCrunch picks up `CustomBuildProperties` (like `UsePackageReference_Compze_Utilities = true`).

## The FlexRef + NuGet Restore Problem

FlexRef.props only parses `.slnx` files (XML regex on `<Project Path="...">`). When building/restoring with a `.sln`, `_FlexRef_SolutionProjects` stays empty. Under NCrunch (`$(NCrunch) = 1`), slnx parsing is also skipped entirely.

In both cases the `UsePackageReference_*` flags remain unset, which *should* default to `ProjectReference`. However, **NuGet restore uses the existing `project.assets.json`**. If that file was previously generated in a context where packages were resolved (e.g. a focused solution), it will contain stale `PackageReference` entries that fail if the packages aren't in the local feed.

### Workaround: Restore via `.slnx` first

```powershell
dotnet restore src/Compze.slnx
```

This regenerates valid `project.assets.json` files with FlexRef evaluated correctly. After that, the NCrunch console tool run succeeds.

## Running Successfully

```powershell
# Step 1: Ensure valid NuGet restore state
dotnet restore src/Compze.slnx

# Step 2: Run NCrunch
NCrunch.exe C:\Dev\Compze\src\CompzeForNCrunch.sln /VS dotnet /O C:\NCrunch\ConsoleResults -LogVerbosity Summary -MaxNumberOfProcessingThreads 4
```

### Useful flags

| Flag | Purpose |
|------|---------|
| `/VS dotnet` | Use dotnet SDK instead of Visual Studio MSBuild |
| `/O <dir>` | Output results directory (HTML reports, XML results) |
| `-LogVerbosity Summary` | Reduce log noise (options: Summary, Normal, Detailed) |
| `-MaxNumberOfProcessingThreads N` | Control parallelism |
| `-Default GridServerReferencesForComputer` | Disable grid (run locally only) |
| `/E "engine mode name"` | Choose engine mode (e.g. "Non-performance") |

### Exit codes

| Code | Meaning |
|------|---------|
| 0 | OK |
| 1 | Build Failure |
| 2 | Test Failure |
| 3 | General Failure |
| 4 | License Failure |
| 5 | Some tests not run |

### Output

Results are written to the output directory:
- `AllResults.html` — full report with coverage
- `TimeLine.html` — task execution timeline
- `RawResults.xml` — machine-readable results
- `TestResultsInNUnitFormat.xml` — NUnit-compatible export
