# Compze Repository — Claude Code Instructions

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

## Detailed Instructions

The sections below are ported from scoped Copilot instruction files. Apply the relevant section based on what files you are working with.

---

## C# Code Conventions (`**/*.cs`)

### Design & Refactoring
- When implementing new functionality, if a missing abstraction makes the implementation inconsistent — **introduce that abstraction**. Refactoring existing code to better accommodate new changes is expected.
- Do not bolt new behavior onto an ill-fitting structure just to avoid creating classes. If the right design calls for a new class, record, interface, or helper — create it.
- **Behavior belongs with data.** If a static method's first parameter is an object it primarily operates on, that method should be an instance method on that type. Prefer `object.DoSomething(...)` over `StaticHelper.DoSomething(object, ...)`. Static utility classes are for genuinely cross-cutting operations, and those should usually be extension methods.
- **Logic belongs where it fits, not where it's first needed.** Move methods to the type they operate on.

### Formatting
- **Indentation**: 3 spaces.
- **File-scoped namespaces**: Always `namespace Foo.Bar;` — never block-scoped.
- **Namespace = folder path**: The namespace must match the project name + subfolder structure.
- **Braces**: Allman style for type and member declarations. No space between keyword and parenthesis in control flow: `if(condition)`, `foreach(var x in items)`, `while(true)`.

### Type Declarations
- **`var` everywhere**: Use `var` for all local variables — even for built-in types.
- **Expression bodies (`=>`)**: Prefer for single-expression methods, properties, constructors, and operators.
- **Primary constructors**: Use when appropriate but create explicit fields for the arguments. Do **NOT** use primary constructor argument capturing.
- **Access modifiers**: Omit default access modifiers. No explicit `private` on fields or methods; no explicit `internal` on classes. Only write modifiers that change the default.
- **`readonly`**: Use on fields for immutable state.

### Naming

| Element          | Convention                                                                 |
| ---------------- | -------------------------------------------------------------------------- |
| Classes          | `PascalCase`                                                               |
| Interfaces       | `I` prefix: `IEndpoint`, `ITessage`                                        |
| Methods          | `PascalCase`                                                               |
| Fields           | `_camelCase` (no explicit `private`)                                       |
| Properties       | `PascalCase`                                                               |
| Constants        | `PascalCase`                                                               |
| Enums/values     | `PascalCase`                                                               |
| Generic params   | `T` prefix: `TEntity`, `TKey`                                              |
| Extension classes | `{TypeName}CE` suffix: `StringCE`, `EnumCE`, `TimeSpanCE`                 |
| Extension 1st param | `@this`: `this string @this`                                           |
| Functional utils | Lowercase for "language-like" helpers: `caf()`, `then()`, `mutate()`, `tap()` |

- **Make names however long they need to be.** The most common problem is that trying to keep names short means they do NOT describe what they do. If you need a comment to explain what a method or variable does, the name should be improved instead.

### Using Directives
- Place at file top, outside namespace.
- Prefer `using static Compze.Contracts.Assert;` to call `Argument.NotNull()`, `State.Is()` etc. without the `Assert.` prefix.

### Null Handling
- **Nullable reference types enabled** in all projects.
- Use Compze.Contracts: `Assert.Argument.NotNull(value)`, `Assert.State.Is(condition)`, `Assert.Result.NotNull(result)`, `Assert.Invariant.Is(condition)`
- Use `.NotNull()` extension for quick null-dereferencing.

### Extension Methods
- **Use extension blocks, not the old `@this` syntax**:
  ```csharp
  extension(ContractAsserter @this)
  {
     public ContractAsserter NotNull<T>([NotNull] T? value, ...) { ... @this.ThrowNull(...); ... }
  }
  ```

### No Records
- **Do not use `record` or `record struct`.** If value equality is genuinely needed, implement `IEquatable<T>` explicitly.

### Default Interface Methods (Mixins)
- This codebase uses default interface methods and extension methods extensively as a mixin pattern. **Always check interfaces for default method implementations AND extension methods** before assuming a method doesn't exist.

### Collections
- Use collection expression syntax `[]` for initialization: `List<Task> tasks = [];`.
- Use immutable types for exposed data: `IReadOnlyList<T>`, `IReadOnlyDictionary<K,V>`, `IReadOnlySet<T>`
- Prefer LINQ method syntax when not cumbersome.
- Target-typed `new()` for well-known types.

### Async
- **`.caf()`** instead of `.ConfigureAwait(false)` — apply to every `await` in library code.

### Strings
- String interpolation `$"..."` everywhere — no concatenation.
- Raw string literals `$"""..."""` for multi-line strings.
- `nameof()` in exception messages.

### Exceptions
- **CRITICAL**: Never swallow exceptions in a catch block without rethrowing.
- Prefer `Assert.*` from Compze.Contracts over manual if/throw.

### Comments & Documentation
- Prefer self-documenting code over comments — extract well-named methods instead.
- XML doc comments: be brief and concise. Omit on obvious/internal code.

### File Organization
- Generally one primary type per file; nested classes and related small types in same file are fine.
- Partial classes split across files using `ClassName.Aspect.cs` naming.
- Underscore-prefix filenames (`_TessageTypes..Interfaces.cs`) for grouped/supporting types.

### Generic Constraints
- Multiple or complex constraints on separate lines, indented 3 spaces.

---

## BDD-Style Specifications (`test/**/*.cs`)

### Preferred Style
All new specifications should use **BDD-style nested specification classes** using `[XF]` (ExclusiveFact).

When adding specs to an existing flat-style class:
- If easy, refactor existing specs to BDD-style, then add new ones.
- If complex, create a new BDD-style class instead.
- Do NOT add new specs to old flat structure.

### Black-box (integration) specs, not unit tests
- No mocking frameworks — use real implementations with the real DI container.
- No direct constructor calls to domain services — resolve through the container.
- **Never make constructors, fields, or methods public just so tests can access them.**
- **Don't duplicate initialization details in tests.**

### Naming: The Single Most Important Rule
Reading *only* the full specification names — from namespace through nested classes to method name — must be enough to correctly implement the specified behavior. No one should need to read the test code.

Example spec lines:
- `Specifications.Contracts.AssertionMethods.NotNull.Throws_when_argument_is.null_string`
- `Specifications.UserAccounts.Registration.When_a_user_attempts_to_register.with_valid_data.registration_succeeds`

### Structure
- Nested classes inherit from parent; each level's constructor performs that level's setup.
- **Folders/namespaces** = categorization. **Nested classes** = accumulated context.
- If a nesting level has no constructor body, it's just categorization — use a folder/namespace instead.

### Key Rules
- **Use `[XF]`**, never `[Fact]` — `[Fact]` causes inherited specs to re-run in every descendant.
- **Class names describe context**: `with_invalid_email`, `that_is_empty`
- **Method names describe expected behavior**: `registration_is_rejected`
- **Each level's constructor is the "act"**
- **Specs at each level are only assertions** — single-expression `=>` bodies calling `Must()`.

### Assertions — Use `Must` Library
```csharp
value.Must().Be(expected);
value.Must().NotBeNull();
collection.Must().HaveCount(5);
Invoking(() => action()).Must().Throw<SomeException>();
await InvokingAsync(async () => await asyncAction()).Must().ThrowAsync<SomeException>();
```

### Setup & Teardown
- Set up state in the **constructor** — not in `[SetUp]` or separate methods.

### Async Specs
- Return `Task` or `async Task`.
- **No `.ConfigureAwait(false)` / `.caf()` in specification code.**

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

## Avalonia AXAML Views (`**/*.axaml`, `**/*.axaml.cs`)

### Compiled Bindings
- Use `x:DataType="vm:XxxViewModel"` on root element of Windows/Dialogs with a ViewModel.
- UserControls may omit `x:DataType` — they receive DataContext from parent.

### DataContext Setup
- **Never** set `DataContext` in AXAML — always in code-behind constructor.
- Use `Design.DataContext` separately for the AXAML previewer.

### Commands vs Event Handlers
- Prefer command bindings (`{Binding MyCommand}`) for all user interactions.
- Use event handlers only when commands are impractical.

### Layout Patterns
- `DockPanel` for top-level, `StackPanel`/`Grid` for sections.
- All styling is inline — no external stylesheet files.

---

## Markdown Documents (`*.md`)

- **Be concise.** State conclusions, not the reasoning journey.
- **No historical baggage.** Rewrite to reflect current state, don't append corrections.
- **No redundancy.** Don't explain the obvious.
- **When editing, check if the document has grown bloated.** Tighten it.

---

## ReSharper Inspections (Reference)

### Running
```powershell
jb inspectcode src/Compze.AllProjects.slnx `
   --output=inspection-results.sarif `
   --severity=SUGGESTION `
   --include="**/*.cs" `
   --exclude="**/_docs/**"
```

### Correcting Issues
- **Group issues by file.** Read each file once, apply all fixes, move on.
- **Build after each batch** (~10-20 files). Catching errors early avoids cascading confusion.
- **Run the full test suite once** at the end after the build is clean.
- **Do NOT blindly apply changes at line numbers** — as you edit files, line numbers shift. Always read the file to find the actual code.

### MemberCanBeInternal Pitfalls
- Override methods: all overrides must also become `internal`.
- Serialized properties: Properties serialized by Newtonsoft must have `public` getter. Making it `internal` causes silent data loss.
- Solution completeness: Analysis is only correct if the solution contains all consumer projects.

---

## Teventive Programming
- Events use interface inheritance for type-based routing.
- Example: `IUserImported : IUserRegistered : IUserEvent : IAggregateEvent`
- Subscribers receive events they're compatible with through type hierarchy.

## Documentation Co-Location
- Documentation lives in `_docs/` folders next to the code it documents.
- See `src/Documentation-CoLocation.README.md` for details.
