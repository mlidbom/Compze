# Compze Repository — Claude Code Instructions

> **Greenfield — there is no production.** Zero deployed applications, no persisted data, and **no backward-compatibility constraints**. Any format, identifier, schema, or behavior may change freely — optimize for the best long-term design, never hedge for migration safety. (Code must still build and the full test suite must still pass.)

## Rules — Follow These First

- **Don't code until instructed to.** Standard workflow is questions back and forth coming up with what to do. Then the go-ahead to code is given. Questions are not instructions to start coding.
- **No changes external to the repo without confirmation.** The repo working tree is free game; anything outside it is not. Pause and ask before editing files outside the repo (`~/.claude.json`, `~/.claude/settings.json`, `~/.bashrc`, OS configs, plugin caches, etc.) or running commands that mutate global state (`claude mcp add --scope user`, `dotnet tool install -g`, `npm i -g`, registry edits, claude.ai account state). Local repo edits, tests, builds, and local git operations don't require this gate.
- **Test thoroughly**: Always run the full test suite before finalizing.
- **Performance tests**: If they fail, rerun. Repeated failures are NOT acceptable — do not report success.
- **`COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY`**: Set to `true` (the default in CI and `C-Test`) to run performance tests as stress tests only, disabling timing assertions. Set to `false` to re-enable timing checks.
- **`COMPOSABLE_MACHINE_SLOWNESS`**: Set this environment variable (e.g., `5.0`) to adjust performance test timing expectations on slow machines. Only applies when stress-test-only mode is off.

### Honesty About Blockers — MANDATORY
- **REFUSE to start work when you lack what you need to succeed.** If the target design is unclear, if you don't know what the end state should look like, if the instructions leave a fundamental gap — SAY SO immediately. Do not guess.
- **Moving files is not separating concerns.** If you would need to add a cross-reference back to make it compile, the dependency hasn't changed — call this out.
- **Ask the design question upfront.** When structural work requires a design decision you cannot make alone — stop and ask.
- **Name what you don't know.** "I don't know how X should work after this change" is always preferable to silently preserving the old architecture and reporting success.
- **Never hide behind "existing architecture."** Existing entanglement is the problem to be solved, not a constraint.

## Workarounds for upstream bugs

Active workarounds live in [CLAUDE.workarounds.md](CLAUDE.workarounds.md). Read it if C# LSP probes start returning "No symbols found" or symbols from the wrong `.slnx`, or if the **PowerShell tool returns `Exit code 1` with no output on every call** (use Bash with `pwsh -NoProfile -NonInteractive -Command "..."` instead). Currently covers: csharp-ls + Claude Code [#16360](https://github.com/anthropics/claude-code/issues/16360), and PowerShell tool failure in the VS Code extension UI mode ([#55671](https://github.com/anthropics/claude-code/issues/55671)).

## Shared Claude config: `.claude-shared/` catalog + `.claude/` symlinks

The C# code conventions, BDD specification style, universal code standards, and markdown rules are **rules under `.claude/rules/`**, most of them symlinks selecting from the `.claude-shared/` catalog — a git subtree shared across repositories (see [.claude-shared/README.md](.claude-shared/README.md)). Start at [.claude/rules/README.txt](.claude/rules/README.txt); shared skills are linked into `.claude/skills/` the same way.

- Sync: `.claude-shared/git-scripts/pull.ps1` / `push.ps1` (run from anywhere).
- The symlinks require Windows Developer Mode and `git config core.symlinks true`. `C-Build` runs `C-Verify-ClaudeConfigSymlinks` and fails loudly if a checkout has degraded them into plain text files.
- The ReSharper inspection workflow reference lives at [.claude-shared/reference/resharper-inspections/](.claude-shared/reference/resharper-inspections/).

## Repository Overview

Compze is a .NET framework for building expressive domains through:
- **Teventive programming**: Type-routed events (also called "Semantic Events") that leverage .NET type compatibility for elegant event modeling
- **Typermedia APIs**: Type-based message routing that extends hypermedia principles with .NET types

## Tech Stack

- **Language**: C# (.NET 10, see `src/global.json`)
- **Testing**: xUnit v3 (via `Compze.xUnit`, `Compze.xUnitBDD`, `Compze.xUnitMatrix`)
- **Build System**: MSBuild (.NET SDK), solution file: `src/Compze.AllProjects.slnx`
- **References**: FlexRef — auto-switches between `ProjectReference` and `PackageReference` depending on which projects are in the current solution
- **Dependency Injection**: Pluggable (Microsoft DI, Autofac)
- **Persistence**: Pluggable (SQLite in-memory, SQL Server, PostgreSQL, MySQL)
- **Serialization**: Pluggable (Newtonsoft)
- **Transport**: Pluggable (AspNetCore)
- **Documentation**: DocFX (site in `src/Websites/Website/`)
- **Development Tools**: PowerShell module (`DevScripts/Compze.psm1`)

## Build and Test

### Prerequisites
- .NET SDK (version specified in `src/global.json`)
- PowerShell with DevScripts module imported: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`

### Building
```powershell
C-Build                  # Build the solution (preferred)
C-Build -Clean           # Deep clean then build
dotnet build src/Compze.AllProjects.slnx  # Alternative: direct .NET CLI
```

### Running Tests
```powershell
# DevScripts (preferred) — C-Test BUILDS BY DEFAULT
C-Test                         # Build + run all tests
C-Test -NoBuild                # Skip building, just run tests
C-Test -SingleThreadedTesting  # Sequential execution (for debugging)
C-Test -Iterations 5           # Run suite 5 times, show summary
C-Test -Clean                  # Deep clean + build + test
C-Test -FullGitReset           # Full git clean + build + test

# Running a subset of tests (no DevScripts support, use dotnet directly)
dotnet test src/Compze.AllProjects.slnx --no-build --filter "FullyQualifiedName~MyTestClass"
```

### Test Configuration
- Config file: `src/TestUsingPluggableComponentCombinations` (auto-created from `.defaults` on first build)
- Format: `PersistenceLayer:DIContainer:Serializer:Transport` (one combination per line, `#` to comment out)
- Default active combination: `SqliteMemory:Microsoft:Newtonsoft:AspNetCore`

## Project Structure

Flat layout — each project has its own top-level directory:
- Library projects: `src/<ProjectName>/<ProjectName>.csproj`
- Test projects: `test/<ProjectName>/<ProjectName>.csproj`
- Multiple `.slnx` files exist for different subsets; `Compze.AllProjects.slnx` is the monolith

### Test Project Naming
- **`.Specifications`** — BDD-style specification projects (preferred for new projects)
- **`.Tests.`** — older integration/unit test projects

## Common Pitfalls
- **Don't write one test per pluggable component** — use `[PCT]` (see Pluggable Component Testing below).
- **DevScripts must be imported** before using `C-*` commands. Import with: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`
- **New package versions**: When creating a new packable project, set `<Version>` to an early pre-release version (e.g., `0.1.0-alpha.1`). **NEVER** use `1.0.0` or any stable-looking version for something new and in development.

---

## Compze-Specific Test Conventions (`test/**/*.cs`)

### Test Attributes
| Attribute | Purpose |
| --- | --- |
| `[PCT]` | Pluggable Component Theory — runs for every configured component combination |
| `[PCTSerializer]` | Varies only the Serializer component |
| `[PCTDIContainer]` | Varies only the DIContainer component |
| `[Performance]` | Marks performance tests |
| `[LongRunning]` | Marks long-running tests |

### Pluggable Component Testing
- **Never** write one test per pluggable component. Use `[PCT]` + `UniversalTestBase` + `TestEnv`.
- Inherit from `UniversalTestBase` for lifecycle management.
- Override `DisposeInternal()`, `InitializeAsyncInternal()`, `DisposeAsyncInternal()` instead of implementing `IDisposable`/`IAsyncLifetime` directly.

### Integration Tests
- Create a `TestingEndpointHost`, register endpoints, start/stop in lifecycle methods.
- Use `IServiceLocator` for resolving services.
- Use `IThreadGate` for controlling concurrency timing.

### Performance Tests
- Use `TimeAsserter.Execute(action, iterations: N, maxTotal: duration)`.
- Use `EnvDivide()` / `EnvMultiply()` on thresholds for slow machines.
- Mark performance test projects with `[assembly: PerformanceAttribute]`.
- Test parallelization is disabled in performance test projects.

---

## FlexRef (`**/*.csproj`, `**/Directory.Build.props`, `**/FlexRef.config.xml`)

FlexRef auto-switches between `ProjectReference` and `PackageReference` depending on which projects are in the current solution. This lets the repo maintain multiple `.slnx` solutions.

### Key Rules
- **Never hand-edit flex reference sections in csproj files** — they are generated by `flexref sync`.
- **Never hand-edit the FlexRef section in `Directory.Build.props`** — generated by `flexref sync`.
- **Always run `C-FlexRef-Sync` after structural changes** — adding/renaming/removing projects or changing references.
- **Focused solutions require `C-Pack` first** — the `PackageReference` path needs packages in `nupkgs/`.
- **The monolithic solution (`Compze.AllProjects.slnx`) needs no packages** — all references resolve as `ProjectReference`.

---

## DevScripts (`DevScripts/**/*.ps1`, `DevScripts/**/*.psm1`)

Import: `Import-Module <repo>/DevScripts/Compze.psm1 -DisableNameChecking`

Discover all commands: `C-Get-Commands` or `C-<Tab>` in PowerShell.

### Output Convention
Do not write output for each step. Success is silent. Only output if the function's purpose is to provide information.

### Key Commands
| Command | Purpose |
|---------|---------|
| `C-Test` | Build + run full test suite |
| `C-Build` | Build the solution |
| `C-Clean` | Deep clean the solution |
| `C-Create-Project` | Create new projects with proper structure |
| `C-Delete-Project` | Delete a project |
| `C-Rename-Project` | Rename a project |
| `C-Split-Project` | Split a project into multiple |
| `C-Merge-Project` | Merge projects |
| `C-FlexRef-Sync` | Sync FlexRef infrastructure after reference changes |
| `C-Validate-SolutionStructure` | Validate solution structure |

---

## Teventive Programming
- Events use interface inheritance for type-based routing.
- Example: `IUserImported : IUserRegistered : IUserEvent : IAggregateEvent`
- Subscribers receive events they're compatible with through type hierarchy.

## Documentation Co-Location
- Documentation lives in `_docs/` folders next to the code it documents.
- See `src/Documentation-CoLocation.README.md` for details.
