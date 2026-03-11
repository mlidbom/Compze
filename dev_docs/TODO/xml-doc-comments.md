# XML Documentation Comments

## Why This Matters

- **AI assistants (Copilot, etc.) read doc comments on declarations** to understand API contracts. Without them, behavior must be inferred from method names and implementation — leading to repeated misuse of threading primitives, gate APIs, etc.
- **Package consumers don't have source.** NuGet packages carry `.xml` doc files that power IDE tooltips — but only if `GenerateDocumentationFile` is enabled. Without it, consumers get no IntelliSense descriptions at all.
- **DocFX generates API reference from doc comments.** The website at compze.net can only document what's annotated in source.
- **MCP documentation servers** (Context7, custom) could index our docs for AI-assisted development — but need doc comments as the source material.

## Current State

**All 51 packable projects now generate XML documentation files** via `GenerateDocumentationFile` in `src/Directory.Build.props`, tied to the packability condition.

### Doc comment coverage

**Good (warnings enforced — no CS1591 suppression):**
- Compze.Contracts, Compze.Unit

**Sparse to none (CS1591 suppressed in .csproj — remove suppression when documented):**
- All other 49 packable projects (see per-project `<NoWarn>CS1591</NoWarn>`)

### How it works
- `src/Directory.Build.props` sets `<GenerateDocumentationFile>true</GenerateDocumentationFile>` for all projects where `IsTestProject != true` and `IsPackable != false`
- Projects without doc coverage suppress CS1591 individually in their `.csproj`
- To "graduate" a project: write doc comments, then remove `<NoWarn>CS1591</NoWarn>` from its `.csproj`

## Action Plan

### Phase 1: Enable `GenerateDocumentationFile` globally — DONE
`GenerateDocumentationFile` is set in `src/Directory.Build.props` for all packable projects. All 49 undocumented projects suppress CS1591 individually. Packages now ship `.xml` files.

### Phase 2: Document high-impact APIs first
Prioritize projects where AI misuse has been observed or where consumer-facing APIs are most critical:
1. **Compze.Threading.Testing** — `IThreadGate`, `IGatedCodeSection` (direct cause of repeated AI errors)
2. **Compze.Threading** — `IMutex`, `IAwaitableLock`, `WaitTimeout`, `LockTimeout`
3. **Compze.Must** — assertion API used everywhere
4. **Compze.DependencyInjection** — `IServiceLocator`, container registration APIs
5. **Compze.Tessaging** — core messaging abstractions

### Phase 3: Systematic coverage
Work through remaining projects, prioritizing public API surface over internals.

### Phase 4: Context7 indexing

Context7 indexes **Markdown files only** — not XML doc comments or `.xml` files. So the pipeline is: XML doc comments → DocFX → Markdown → Context7.

**How to submit:**
1. Go to [context7.com/add-library](https://context7.com/add-library), paste the GitHub repo URL
2. Add a `context7.json` to the repo root to control what gets indexed:
   ```json
   {
     "$schema": "https://context7.com/schema/context7.json",
     "projectTitle": "Compze",
     "description": "Framework for building expressive .NET domains through teventive programming and typermedia APIs",
     "folders": ["docs", "src/**/README.md", "src/**/_docs"],
     "excludeFolders": ["test", "**/bin", "**/obj", "nupkgs"],
     "rules": [
       "Use IThreadGate for deterministic thread testing — AwaitLetOneThreadPassThrough leaves the gate closed",
       "Use [PCT] attribute for pluggable component tests, not one test per component"
     ]
   }
   ```
3. Claim ownership for admin panel, version management, and team access

**Key insight:** The `rules` field injects best-practice hints into AI context when consumers use the library via Context7. This is a direct channel to prevent the kind of misuse we see with gates/sections.

**Prerequisites before submitting:**
- README files on all key projects (many already exist, e.g. `Compze.Threading.Testing/README.md`)
- DocFX-generated Markdown API docs committed to the repo (requires Phase 1-3 first)
- Co-located `_docs/` folders with usage guidance
