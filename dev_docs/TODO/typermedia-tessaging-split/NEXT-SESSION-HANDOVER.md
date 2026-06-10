# Handover: finish the typermedia–tessaging split (B3 + documentation)

For the next Claude session. Delete this file when everything below is done. Read
`typermedia-tessaging-split-v3.md` (same folder) first — it is the accurate current-state record. This file adds
the marching orders, decisions already made, and hard-won gotchas so nothing is relitigated.

## Mandate from Magnus (verbatim intent)

1. Continue per own judgment until "nothing major to do here anymore". Full authority on design decisions;
   conceptual coherence and truthful naming outrank convention and minimalism (see his global rules).
2. Give all involved types and members detailed XML documentation comments with rich `<see>` cross-linking, good
   enough that a newcomer to the code AND its patterns understands it from the docs.
3. Write a `.md` explaining the hosting model as it exists NOW — zero references to what it used to be. Audience:
   a new developer maintaining Compze, going from zero to understanding production + testing hosting.

## Current state (all committed, branch `separate-typermedia-and-tessaging`, suite green 3151/0)

- Production code fully disentangled. `Compze.Hosting` is a paradigm-blind mechanism (references NO paradigm, no
  `InternalsVisibleTo` anywhere to it). Paradigms plug in as features:
  - Seam: `IEndpointBuilder.GetOrAddFeature<TFeature>()` / `AddComponent()` / `OnContainerBuilt()` +
    `IEndpointComponent` lifecycle contract — all in `Compze.Abstractions.Hosting.Public`.
  - `TessagingEndpointFeature`/`-Component`/`EndpointBuilderTessagingExtensions`/`EndpointTessagingExtensions`
    in `src/Compze.Tessaging/Hosting/`. `AddTessaging()`, `RegisterTessagingHandlers`, `TessagingAddress`
    (extension property on `IEndpoint`, reads its component, null until listening).
  - `TypermediaEndpointFeature`/`-Component`/extensions in `src/Compze.Typermedia.Client/`. Same shape.
  - `TestingEndpointHost` (`src/Compze.Tessaging.Hosting.Testing/Tessaging/Buses/`) absorbed the old base class:
    owns the `TessagesInFlightTracker`, implements `IEndpointRegistry`, pre-registers tracker instance +
    `CurrentTestsPluggableComponents` + `AddTessaging()` + `AddTypermedia()` for every endpoint before user setup.
    Feature defaults use `IComponentRegistrar.IsRegistered<T>()` guards (tracker → NullOp, registry → AppConfig
    stub) so host pre-registrations win.
- `Compze.Typermedia.slnx` exists; the three Typermedia spec projects and `Compze.Typermedia.Hosting.Testing`
  are placeholders.

## B3 — the remaining structural work: testing-infrastructure split

`Compze.Tessaging.Hosting.Testing` is the last fused area (it references Typermedia). Inventory of what it holds
(`Wiring/`): `TestingComponentRegistrar` (a `ComponentRegistrar` subclass wiring per-SQL-provider testing
registrars; referenced by `DiContainerExtensions.CreateEmpty` for all four DI containers),
`TestingComponentRegistrar.{DbPool,Serializer,TestingSqlLayerRegistrar,ClientTransport,Transport,
TestingComponentsRegistrar}.cs` partial-extension files, `DiContainerExtensions`, `ContainerCloner`,
`DummyConfigurationParameterProviderRegistrar`, `TypeIdentifierMapperTestRegistrar`, plus `PluggableComponents.cs`,
`TestEnv.cs`(? — verify: `TestEnv` may be in `Compze.Internals.Testing`; the enums `DIContainer`/`Serializer`/
`SqlLayer`/`Transport` are in `Compze.Abstractions/Wiring/Testing/Internal/`), `Tessaging/TestClient.cs`,
`Tessaging/Buses/TestingEndpointHost.cs`.

Decided direction (lay-out; refine while implementing):

1. **New package `Compze.Hosting.Testing`** (use `C-Create-Project`; version `0.1.0-alpha.1`-style — NEVER 1.0.0):
   the paradigm-neutral pluggable-component test wiring — `TestingComponentRegistrar` + DbPool/serializer/
   SQL-layer partials, `DiContainerExtensions`, `ContainerCloner`, dummy config provider. It will reference the
   pluggable matrix (DI providers, SQL providers, serializers) — that breadth is its purpose; do NOT flag it as
   bloat. Transport wiring does NOT go here (paradigm-specific).
2. **`Compze.Typermedia.Hosting.Testing`** (fill the placeholder): `TestClient` (rename? it is a remote Typermedia
   test client — consider `TypermediaTestClient`), the typermedia client-transport test wiring
   (`CurrentTestsClientTransport` equivalent), `TypeIdentifierMapperTestRegistrar`(? it maps all four framework
   assemblies incl. Core/Tessaging — decide: maybe it belongs in the combined layer; a Typermedia-only variant
   would map only Abstractions+Internals.Transport+Typermedia.Client), and a Typermedia-only testing host
   (compose `EndpointHost` + `AddTypermedia()`).
3. **`Compze.Tessaging.Hosting.Testing`** keeps a Tessaging-only testing host + tessaging transport test wiring;
   loses all Typermedia references.
4. **The combined host** (current `TestingEndpointHost`): needed by `Compze.Tests.*` and `samples/`. Options:
   (a) keep a combined-composition testing package (e.g. `Compze.Hosting.Testing.AllParadigms` — ugly name, think);
   (b) move the combined host to `test/Compze.Tests.Common` (not packable) and have samples compose their own
   tiny host from the two paradigm testing packages — composing `AddTessaging()+AddTypermedia()` explicitly in the
   sample is good documentation-by-example. Leaning (b); judge while implementing.
5. **Proofs of done:** real specs (BDD `[XF]`/`Must` style per CLAUDE.md) in the three Typermedia placeholder spec
   projects exercising a Typermedia-only host end-to-end (host + `TestClient` over HTTP, no Tessaging assembly in
   the dependency closure — assert that in a spec if cheap); new `Compze.Tessaging.slnx` mirroring
   `Compze.Typermedia.slnx` that builds + tests with no Typermedia project. Focused slnx builds need
   `C-Pack` + packages for package-mode references — verifying via `Compze.AllProjects.slnx` build + the spec
   projects running in the full suite is the practical bar; creating the slnx files is still required.

## Documentation work (Magnus's items 2 and 3)

- XML docs with `<see>`-rich cross-links on: the five contracts in `Compze.Abstractions/Hosting/Public/`
  (`IEndpoint`, `IEndpointHost`, `ITestingEndpointHost`, `IEndpointBuilder`, `IEndpointComponent`,
  `IEndpointRegistry`, `EndpointAddress`, `EndpointConfiguration`, `EndpointId`), the mechanism
  (`Endpoint`, `EndpointHost`, `ServerEndpointBuilder`, `AppSettingsJsonConfigurationParameterProvider`), both
  features + components + extension classes, `TestingEndpointHost`(+ successors), `TestClient`,
  `AspNetCoreTransportRegistrar`, `AspNetCoreTypermediaTransportServerRegistrar`. Document the PATTERN in the
  docs (what a feature is, why extension properties, lifecycle phase ordering, why null-before-listening
  addresses), not just the member. Keep CLAUDE.md's "brief and concise" in tension with Magnus's explicit ask for
  detail here — his explicit ask wins for these types.
- Hosting-model doc: `src/Compze.Hosting/_docs/hosting-model.md` (co-location convention). Cover: the three-layer
  model (contracts in Abstractions / mechanism in Compze.Hosting / paradigm features in their packages); endpoint
  lifecycle phases and why listening precedes sending host-wide; how a feature wires in (GetOrAddFeature,
  registrations, OnContainerBuilt, AddComponent); addresses as paradigm extension properties; production hosting
  (`EndpointHost.Production.Create`) vs testing hosting (testing host pre-registrations, tracker, IsRegistered
  guards, pluggable components); how to add a new paradigm/feature (the newbie test). ZERO history. After adding
  `_docs`, run `DevScripts\C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders.ps1` (or
  verify csproj excludes `_docs` `.cs`; `.md` Content include is the maintained pattern).
- Update `typermedia-tessaging-split-v3.md` status when done; also the Follow-ups list there is the
  taste-pass backlog (leave for Magnus unless trivial).

## Gotchas this session paid for (do not rediscover)

- **CRLF:** Write tool emits LF; Git-Bash `sed -i`/`awk` rewrites convert whole files to LF. `unix2dos` every
  touched file before committing (raw-string message specs break otherwise). Verify: `file <f> | grep -v CRLF`.
- **FlexRef:** AutoDiscover mode — csproj flex blocks are source of truth. Remove dep = delete its 7-line block,
  run `dotnet flexref sync .`. Add dep = insert a well-formed block (alphabetical position) with CORRECT
  `..\X\X.csproj` path — sync silently DELETES malformed/unresolvable blocks. Never sed-insert with escaped
  backslashes (they get mangled); use the Edit tool.
- **New projects:** `C-Create-Project` (DevScripts; import module first), then `C-FlexRef-Sync`,
  `C-Validate-SolutionStructure`. PowerShell tool may be broken in this environment — use Bash +
  `pwsh -NoProfile -NonInteractive -Command "Import-Module ./DevScripts/Compze.psm1 -DisableNameChecking; ..."`.
- **Rider/ReSharper MCP:** trust the compiler over post-edit IDE diagnostics — the daemon lags on new/moved files
  (phantom "Cannot resolve symbol"). `resharper-joshua` works; pass `solutionName:'Compze.AllProjects'`.
- **Extension properties (C# 14 extension blocks)** are the established pattern for the paradigm extensions; one
  receiver per static class (two extension blocks with different receivers in one class triggers CA1708).
- **Two same-named registrations conflict** in the DI abstraction; use `IsRegistered<T>()` guards for defaults
  the host may pre-register (tracker, endpoint registry).
- **Order in `TestingEndpointHost.RegisterEndpoint`:** host pre-registrations MUST precede `AddTessaging()` (its
  guards), which precede user `setup(builder)`.
- `dotnet build src/Compze.AllProjects.slnx` ~20–60s; full suite via
  `pwsh ... C-Test -NoBuild` ~40s, run in background. Performance-test failures: rerun before judging.
- Samples are part of the AllProjects solution; greps and call-site updates must include `samples/`.
- `Navigator_specification` injects `IServiceBusSession` (Tessaging) inside a *Typermedia* handler — endpoints in
  the combined suite genuinely need both features.
- Production `EndpointHost` endpoints fall back to `AppConfigEndpointRegistry` whose lookup throws
  `NotSupportedException` — pre-existing; production multi-endpoint hosting has never worked. Don't "fix"
  silently; it's in the Follow-ups list.

## Definition of "nothing major to do here anymore"

B3 done (three testing packages + combined-host decision executed, samples and tests updated), proofs of done in
place, XML-doc pass done, hosting-model doc written, v3 status updated, this file deleted, full suite green,
0 warnings, structure validation clean, all committed.
