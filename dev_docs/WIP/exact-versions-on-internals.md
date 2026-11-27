# Exact versions of the Compze packages whose internals a package uses: the design

**Status:** design only, awaiting Magnus's answers to the three decisions at the end. Nothing is built. It answers topic 72
of compze-agent-rep, where Magnus chose option 1 on 2026-11-27: a Compze package declares an exact version of each Compze
dependency whose internals it uses, and only those.

## The headline

- **A Compze package will require an exact version, `[x.y.z]`, of each Compze package whose internals it uses,** and a
  minimum version of the others, as now. Which packages it uses the internals of is read from the `InternalsVisibleTo`
  items in the project files, as they are at the commit being packed.
- **The .NET SDK's pack writes the exact versions itself,** through one MSBuild target of about twenty lines in
  `Directory.Build.targets`. Proven: packing the whole solution with it gave the 69 exact versions that the grants call
  for, in 41 packages, and changed nothing else. A consumer that restores Compze.Teventive 0.9.1 beside the
  Compze.Teventive.MemoryImageStateStore built that way gets NU1608.
- **`C-Prepare-Release` releases every package that uses the internals of one it releases,** as a patch with a generated
  changelog line, and the release refuses a set of versions that leaves one behind.
- **The cost: about 2.3 times as many package versions.** The last 20 releases published 197. With exact versions they
  would have published 445. Compze.Sql.Common causes most of it: each release of it would release 29 more packages.
- **Three decisions are Magnus's,** at the end: whether to go ahead at this cost, the changelog line, and NU1608 in the
  repositories that use Compze.

## Terms

- **Exact-version dependency:** a dependency that a package's manifest (its `.nuspec`) declares with one version,
  `[x.y.z]`, which NuGet satisfies with that version only. The design doesn't say "pin": the upgrade report already says
  "pinned" for Central Package Management's transitive pinning, which is another thing.
- **Uses the internals of:** package A uses the internals of package B when B's project grants A `InternalsVisibleTo`
  (`<InternalsVisibleTo Include="A" />`) and A's project references B's project directly.

## Why

Compze.Teventive.MemoryImageStateStore 0.2.1 uses Compze.Teventive's internals. Its manifest declares a minimum version of
Teventive, so restoring it beside Teventive 0.9.0 succeeds, and the store breaks when it runs. Internals carry no
compatibility promise: any release of the package that grants them can break any package that uses them.

With an exact version, only the pairing built from one commit restores without a word from NuGet:

- **NU1608, a warning,** when the consumer references both packages directly, as the tooling does: "Detected package version
  outside of dependency constraint".
- **NU1107, an error,** when two packages require different exact versions of a third, and the consumer doesn't reference
  the third directly.

## 1. Where "uses the internals of" comes from

**The `InternalsVisibleTo` items of each project that the package references directly, as MSBuild evaluates them while it
packs the package.** They are the items the SDK compiles into the granting assembly's `InternalsVisibleToAttribute`, so the
exact versions follow what the assemblies allow, with no second list to keep in step.

- **Test and specification projects drop out by themselves.** A grant to one names a project that is never packed, so no
  manifest has a dependency to make exact.
- **The grants at HEAD:** 93 between the 73 packages that are published to nuget.org, from 16 packages to 41. The 134 in
  topic 72 also count the grants to test and specification projects.

### Direct references only

Pack writes dependencies only for the projects a package references directly. 24 of the 93 grants go to packages that
reach the granting package only through another one. All 24 are Compze.Sql.Common's, to packages such as
Compze.DbPool.MicrosoftSql that reference Compze.Sql.MicrosoftSql, which references Sql.Common. Their source uses
Sql.Common's `_internal` namespaces, so the grants are real.

- **Each of the 24 gets a direct reference to Compze.Sql.Common,** a FlexRef reference like the others. That says what is
  true: they compile against Sql.Common's internal types. For consumers nothing changes but the range: they already get
  Sql.Common through Sql.MicrosoftSql.
- **A code policy keeps it so.** Compze.Tests.CodePolicies gains a test beside `TestProjectInternalsVisibleToPolicy`: a
  library that another library grants `InternalsVisibleTo` references that library directly. It reads the grants from the
  assemblies, as that policy does, and the references from the project files. A grant to a library that doesn't use the
  granting one fails it too. That is the right outcome, since an unused grant would now cost releases.

### Grants that may be unused

For 7 of the grants, the grantee's source names none of the granting package's `_internal` or `_private` namespaces:

- Compze.DependencyInjection to Compze.Teventive.MemoryImageStateStore;
- Compze.Sql.MicrosoftSql, Compze.Sql.MySql, Compze.Sql.PostgreSql and Compze.Sql.Sqlite to Compze.Tessaging.Hosting.Testing;
- Compze.Tessaging.Abstractions to Compze.Tessaging and to Compze.Teventive.

That is a heuristic: a grantee may use an internal member of a public type instead. When building, I remove each grant
that the build shows unused. It saves little: 7 of the 248 extra versions in section 5.

## 2. How the manifest gets `[x.y.z]`

**How the versions are written now:** `C-Pack` packs `Compze.AllProjects.slnx`, where every Compze dependency is a
`ProjectReference`. FlexRef switches to `PackageReference` only in the focused solutions, and nothing packs those. Pack's
`_GetProjectReferenceVersions` target asks each referenced project for its version through `_GetProjectVersion`, which
returns `PackageVersion` as the `ProjectVersion` metadata of an item of `_ProjectReferencesWithVersions`. The pack task
writes the dependency from that.

**The pack task parses that metadata as a version range.** In SDK 11.0.100, `PackTaskLogic.InitializeProjectDependencies`
in `NuGet.Build.Tasks.Pack.dll` does `VersionRange.Parse(projectVersion, allowFloating: false)`. A plain version reads as a
minimum version, as now, and `[0.9.0-alpha]` reads as an exact one.

**The target,** for `Directory.Build.targets`, as proven under experimental names:

```xml
<!-- Exact versions of the Compze packages whose internals this package uses: see dev_docs/WIP/exact-versions-on-internals.md -->
<Target Name="CompzeGetInternalsVisibleToGrantees" Returns="@(_CompzeInternalsVisibleToGrantees)">
  <ItemGroup>
    <_CompzeInternalsVisibleToGrantees Include="$(MSBuildProjectFullPath)" Grantees=";@(InternalsVisibleTo);" />
  </ItemGroup>
</Target>
<Target Name="CompzeRequireExactVersionsOfTheDependenciesWhoseInternalsThisPackageUses"
        AfterTargets="_GetProjectReferenceVersions" Condition="'@(_ProjectReferencesWithVersions)' != ''">
  <MSBuild Projects="@(_ProjectReferencesWithVersions)" Targets="CompzeGetInternalsVisibleToGrantees">
    <Output TaskParameter="TargetOutputs" ItemName="_CompzeReferencedProjectGrantees" />
  </MSBuild>
  <ItemGroup>
    <_CompzeReferencesNotGrantingThisPackage Include="@(_CompzeReferencedProjectGrantees)"
        Condition="!$([System.String]::Copy('%(Grantees)').Contains(';$(AssemblyName);'))" />
    <_CompzeReferencesGrantingThisPackage Include="@(_ProjectReferencesWithVersions)" Exclude="@(_CompzeReferencesNotGrantingThisPackage)" />
    <_ProjectReferencesWithVersions Remove="@(_CompzeReferencesGrantingThisPackage)" />
    <_ProjectReferencesWithVersions Include="@(_CompzeReferencesGrantingThisPackage)"
        ProjectVersion="[%(_CompzeReferencesGrantingThisPackage.ProjectVersion)]" />
  </ItemGroup>
</Target>
```

It runs only when a package is packed, never in a build, so builds, the IDEs and NCrunch don't run it.

**Proven on one package,** Compze.Teventive.MemoryImageStateStore, packed with the target supplied from outside the
repository (`CustomAfterMicrosoftCommonTargets`). Its manifest:

```xml
<dependency id="Compze.Abstractions" version="0.5.0-alpha" exclude="Build,Analyzers" />
<dependency id="Compze.Contracts" version="0.7.2" exclude="Build,Analyzers" />
<dependency id="Compze.DependencyInjection" version="[0.10.0-alpha]" exclude="Build,Analyzers" />
<dependency id="Compze.Internals.SystemCE" version="[0.13.0-internal]" exclude="Build,Analyzers" />
<dependency id="Compze.Tessaging.Abstractions" version="0.8.0-alpha" exclude="Build,Analyzers" />
<dependency id="Compze.Teventive" version="[0.9.0-alpha]" exclude="Build,Analyzers" />
<dependency id="Compze.Threading.Interprocess" version="0.11.0-alpha" exclude="Build,Analyzers" />
<dependency id="Compze.Threading" version="0.14.0-alpha" exclude="Build,Analyzers" />
```

The three exact ones are the three projects that grant the store `InternalsVisibleTo`. The symbols package has the same
dependencies. A throwaway consumer, restoring this store beside a Teventive packed as 0.9.1-alpha, with a NuGet cache of its
own, got: `warning NU1608: Detected package version outside of dependency constraint:
Compze.Teventive.MemoryImageStateStore 0.4.0-alpha requires Compze.Teventive (= 0.9.0-alpha) but version Compze.Teventive
0.9.1-alpha.pinproof was resolved.`

**Proven on the whole solution:** `dotnet pack Compze.AllProjects.slnx` with the target gave 74 packages, with 69
exact-version dependencies in 41 packages: exactly the 69 direct grants, and no other dependency changed. A local dev pack
carries its suffix into them, such as `[0.13.0-internal.dev.20261127073340]`.

### What it relies on, and the other way

- **It relies on what NuGet doesn't document:** the target `_GetProjectReferenceVersions`, the item
  `_ProjectReferencesWithVersions`, and the range parsing of `ProjectVersion`. NuGet has no supported way to declare a range
  for a project reference: [NuGet/Home#5556](https://github.com/NuGet/Home/issues/5556) asks for one.
  - **If NuGet renames the target or the item,** the target does nothing, and packs get minimum versions again. Check (a)
    in section 4 refuses such a set of packages, so it can't be released.
  - **If NuGet parses `ProjectVersion` as a version again,** pack fails on `[0.9.0-alpha]`.
- **The other way:** rewrite each manifest inside the packed `.nupkg`, as `Write-ChangelogSectionsIntoPackages` already does
  for the release notes. It relies on nothing undocumented. But it is a second writer of the manifest that only packs
  through `C-Pack` get, and it needs its own reading of the grants.
- **Chosen: the target.** MSBuild's own evaluation of the grants is the truest source. Every pack carries the exact
  versions, `dotnet pack`, the symbols packages and the local dev packs included. Check (a) makes a failure loud. The
  rewrite is the fallback if NuGet breaks the target.

## 3. What the release preparation does

**`C-Prepare-Release` adds the packages that use the internals of what it prepares.** After it has chosen the packages to
prepare (their changelogs gained entries) and found those already awaiting release, it adds every package at a released
version that uses the internals of one of them. It repeats this for what it added, until nothing more is added.

- **It reads who uses whose internals from the project files,** by the rule the pack target follows: a grant and a direct
  reference. Check (a) keeps the two the same.
- **An added package gets a patch bump,** since its own code didn't change, and a new changelog section that holds only the
  generated line below. A package prepared for its own entries keeps its own bump, and gets the line too.
- **`-PackageId` doesn't limit what it adds:** a package named there is prepared with every package that uses its internals.
  `-PatchPackageId` may name the added packages. The output gives each package's reason: its changelog, or the packages
  whose internals it uses.
- **The unreleased-changes refusal counts the added packages as prepared,** as it counts the others.

**The generated changelog line** is computed after packing, as the dependency floors line is: against the package's
previous version on nuget.org, it names each exact-version dependency whose version changed. Proposed wording (decision 2):

> - **Requires exact versions of the Compze packages whose internals it uses:** Compze.Sql.Common 0.10.0-alpha (was
>   0.9.0-alpha). Upgrading to this version upgrades each package listed here to exactly that version, so their changelogs
>   apply to this upgrade as well.

- **The dependency floors line leaves out exact-version dependencies,** so that no dependency is named twice in one section.
- **The release checks the line** as it checks the floors line (`ReleaseDetails.ps1`): it refuses when the line is missing,
  is wrong, or is there when nothing changed.
- **Where the code goes:** the line's lead, its formatting and its search beside the floors line's, in a lib file of their
  own, `.github/scripts/lib/ExactVersionDependencies.ps1`, with the checks of section 4.

**An example, at HEAD:** a patch of Compze.Sql.Common would release Sql.Common and 29 more packages: the four
Compze.Sql providers; the Compze.DbPool packages of Microsoft SQL Server, MySQL and PostgreSQL; the DocumentDb, Tessaging,
TeventStore, MemoryImageStateStore and TypeIdentifiers.Interning packages of each of the four databases; and
Compze.Hosting.Testing and Compze.Tessaging.Hosting.Testing.

### What it does to "Keep main releasable" in the skill releasing-compze-packages

The section stays, and covers more:

- **A change to a package whose internals other packages use is such a change.** Any release from main while that change is
  unreleased releases those packages as well: 29 of them for Compze.Sql.Common, 32 for Compze.Internals.SystemCE. The
  section says so, and that `C-Prepare-Release` adds them itself, so this costs releases, not hand work.
- **Its sentence on Create Releases gains a clause:** besides refusing to leave a changed package behind, it refuses to
  leave behind a package that uses the internals of one it releases.
- **An acknowledged change keeps its package unreleased,** and so releases none of the packages that use its internals.
- **`C-Get-PackagesChangedSinceTheirRelease` shows, for each changed package, how many packages its release would release,**
  so that the cost is seen before the preparation.

Step 1 of the skill, and the rule on releasing, say that `C-Prepare-Release` also prepares the packages that use the
internals of those it prepares.

## 4. What checks it

**Three checks in `ExactVersionDependencies.ps1`, called from `GetPackagesToRelease` (`ReleaseSelection.ps1`).** Create
Releases, `C-SimulateRelease`, `C-Publish-ToPendingReleaseFeed` and the post-release binding check's script all select
through that function, so all of them refuse alike, listing every failure:

- **(a) The packages require exactly what the grants say.** Each packed package's exact-version dependencies are the Compze
  packages whose projects grant it `InternalsVisibleTo`: no more, no fewer. This catches the target doing nothing after a
  NuGet change, and a grant to a package that doesn't reference the granting one directly.
- **(b) No exact version that the release doesn't publish and nuget.org doesn't serve.** Each exact-version dependency of a
  package the release publishes names a package the release publishes, at that version, or nuget.org's newest version of
  that package. The newest, not just any version on nuget.org: a consumer that takes the newest of each would otherwise
  get NU1608.
- **(c) No package left behind.** No package the release leaves at its released version uses the internals of a package
  the release publishes. It is read from the fresh packs, not from nuget.org's manifests, so it holds for the released
  versions that predate exact versions too: the first releases after this lands release whatever uses the internals of
  what they release.

**Why that is enough:** an exact-version dependency on a package the release doesn't publish names a build of the source
that the package was compiled against. The unreleased-changes refusal guarantees that: the source at the commit is the
source of that package's released version, but for acknowledged changes. And (b) and (c), at every release, keep each
exact version that nuget.org serves satisfied by nuget.org's newest versions.

**The post-release binding check needs no change.** Its tool is `.github/scripts/PostReleaseBindingCheck`, with its
specifications in `.github/scripts/PostReleaseBindingCheck.Specifications`, not under `test/`. By (c), every package that
uses the internals of a released one is in the release, and the tool goes on checking references and type loading across
the set. A check in it that the set satisfies every exact version would repeat (b) and (c), so I left it out.

**`C-SimulateRelease` shows them:** `Publish-NuGetWithReleases.ps1 -DryRun` lists each package's exact-version dependencies
under its name.

**The manifest reader must come first.** `ReadCompzeDependencyFloors` (`PackageManifests.ps1`) throws on any range today.
So `C-Prepare-Release`, the release, the pending-release feed and the upgrade report would fail on the first manifest with
an exact version. It learns `[x.y.z]`: `CompzeDependencyFloors` keeps a floor for every Compze dependency, the exact version
for an exact one, and a new `CompzeExactVersionDependencies` names the exact ones. Any other range still throws.

## 5. The cost, measured

**Method:** a replay over the last 20 release commits, the commits with release tags, from 2026-11-12 to 2026-11-27. At
each, it read the project files, worked out who uses whose internals, and added to the tagged packages every package that
uses the internals of one of them, repeatedly. It counts the 24 indirect Sql.Common grants as the direct references they
will be. Each package added is one more version published.

| Release commit | Date | Packages released | Exact versions would add |
|---|---|---:|---:|
| 966e54b96 | 2026-11-27 | 1, Tessaging.Hosting.Testing | 0 |
| 0200bffff | 2026-11-27 | 23 | 15 |
| 81f775b23 | 2026-11-27 | 2, Internals.SystemCE and Sql.Common | 31 |
| 3cfcaf4b9 | 2026-11-27 | 16 | 24 |
| 80a22f8bd | 2026-11-26 | 16 | 20 |
| 6d6c63189 | 2026-11-26 | 4 | 6 |
| d2ee13be8 | 2026-11-26 | 4, Abstractions, Tessaging.Abstractions, Tessaging and Teventive | 19 |
| d87b3bf20 | 2026-11-26 | 1, Sql.Sqlite | 7 |
| a0217d8d1 | 2026-11-26 | 5 | 5 |
| 36e0f2067 | 2026-11-26 | 2, Internals.SystemCE and Threading.Interprocess | 32 |
| 1d5b18c7b | 2026-11-26 | 1, Internals.SystemCE | 32 |
| 789b987db | 2026-11-25 | 33 | 16 |
| de778649a | 2026-11-23 | 1, Must | 0 |
| 022aa32bb | 2026-11-19 | 17 | 20 |
| aa880c122 | 2026-11-14 | 65 | 0 |
| 3aff4bd80 | 2026-11-14 | 1, Tessaging | 7 |
| fbe5399d4 | 2026-11-14 | 1, xUnitMatrix | 0 |
| ad16fca2a | 2026-11-13 | 1, Teventive | 1 |
| fc42b8ef7 | 2026-11-13 | 1, Tessaging | 7 |
| c6f376727 | 2026-11-12 | 2 | 6 |
| **All 20** | | **197** | **248** |

**197 versions would have been 445: 2.26 times as many.**

**What a release of one package alone would release at HEAD,** itself included, for each package that grants its
internals:

| Package | Releases |
|---|---:|
| Compze.Internals.SystemCE | 33 |
| Compze.Sql.Common | 30 |
| Compze.Tessaging.Abstractions | 17 |
| Compze.DependencyInjection, Compze.Sql.MicrosoftSql, Compze.Sql.MySql, Compze.Sql.PostgreSql | 9 each |
| Compze.Sql.Sqlite, Compze.Tessaging, Compze.Teventive | 8 each |
| Compze.Abstractions, Compze.Teventive.TeventStore.Abstractions | 7 each |
| Compze.Teventive.MemoryImageStateStore | 6 |
| Compze.DocumentDb | 5 |
| Compze.xUnit | 3 |
| Compze.Threading | 2 |

**What would reduce it:**

- **Removing the 7 grants that may be unused:** 248 becomes 241.
- **Removing SystemCE's 2 grants as well:** 183. Compze.Sql.Common and the store use two of SystemCE's internal types,
  `DurableResource` and `DurableResourcesOfTransactions` (`TransactionsCE._internal`). So a release of SystemCE releases
  Sql.Common, whose release releases 29 more. Making the two types public in SystemCE would remove both grants, but a
  public type must be designed for every consumer (decision 1).
- **Compze.Sql.Common's internals, which 27 packages use,** cause most of the rest. Making what they share a public API of
  the Sql family would remove most of it. That is a design of its own, and not proposed here.

**Not measured:** how long Create Releases takes per package version. I have no timing of it.

## 6. What else it touches

- **The upgrade report (`C-Get-CompzeUpgradeReport`).**
  - **It reads dependency versions as plain versions,** from the restore graphs and from the manifests. Both readers learn
    `[x.y.z]`, or the report fails on the first exact version.
  - **An upgrade brings along an exact-version dependency at exactly its version:** "-> 0.10.0-alpha exactly", not "or
    newer".
  - **Its left-behind section reads a left-behind version's exact-version dependencies from its manifest:** such a pairing
    restores with NU1608, or fails with NU1107, and the section says which. For versions released before exact versions,
    it keeps today's inference from the grants at the build commit.
- **The skill upgrading-compze-packages,** which is user-level, so the tooling owner's: what NU1608 and NU1107 on a Compze
  exact-version dependency mean, and that the fix is to upgrade the package named to the version required, never to
  suppress the warning. I'll send it to the tooling owner when this is built.
- **The pending-release feed.** It publishes the release's packages with their exact-version dependencies, which name
  versions in the feed or on nuget.org: check (b). One change: `-Replace` republishes a version whose package source
  changed. A package that uses its internals was built against the old source, so it is republished too.
  `Test-PackageSourceChangedBetweenCommits` then counts, for a package, the sources of the packages whose internals it
  uses as well.
- **FlexRef's focused solutions: no change.** Their restores take Compze packages from `nupkgs/`, through `nuget.config`'s
  package source mapping. Those are all from one `C-Pack`, so every exact version is satisfied.
  - **Measured:** after a full pack with exact versions, restoring Compze.Tessaging.slnx, Compze.Core.slnx,
    Compze.Abstractions.slnx and Compze.DependencyInjection.slnx gave no NuGet warning.
  - **Where a package requires an exact version of a project in the solution, NuGet takes the project without checking the
    range.** Compze.Serialization.Newtonsoft requires Compze.Tessaging `[0.14.0-alpha.dev.20261127073340]`, and in
    Compze.Tessaging.slnx that resolved, silently, to the Compze.Tessaging project, 0.14.0-alpha. So focused solutions
    restore as they do now.
- **NCrunch: no change.** It builds from the focused solutions' restores and never packs.
- **Builds: no change.** The target runs only when packing.

## 7. The decisions that are Magnus's

### Decision 1: go ahead at this cost?

The cost is about 2.3 times as many package versions per release, 248 more over the last 20 releases.

- **Option A: yes, as designed.** *Recommended.* Releasing more costs no hand work: `C-Prepare-Release` and the release do
  it. In return, only pairings built from one commit restore without a word from NuGet.
- **Option B: yes, and also make SystemCE's `DurableResource` and `DurableResourcesOfTransactions` public,** designed as
  public API, removing SystemCE's two grants. The extra versions over those 20 releases would have been 183. It is a design of its own, and fits after this
  lands as well as before.
- **Option C: no.** Back to the options of topic 72.

### Decision 2: the changelog line for exact-version dependencies

It is what nuget.org shows as part of each version's release notes.

- **Option A: a line of its own, worded as in section 3, and the floors line leaves out exact-version dependencies.**
  *Recommended:* each dependency is named once, and the line says the one thing a consumer needs, that the version is
  exact.
- **Option B: one line for both.** The floors line names the exact-version dependencies too, marked "exactly".
- **Option C: both lines, as they are,** with a dependency that is both exact and raised across a minor version named twice.

Change the wording if you like: it is a proposal.

### Decision 3: NU1608 in the repositories that use Compze

With exact versions, a repository that references both packages directly, as the tooling does, gets a warning, not an
error, for a pairing that breaks at run time.

- **Option A: it stays a warning.** *Recommended.* The upgrade report names the package to upgrade, and the skill
  upgrading-compze-packages says to fix it, never to suppress it. NU1608 can't be made an error for Compze packages alone:
  it would be one for every package's ranges.
- **Option B: each such repository makes NU1608 an error** (`<WarningsAsErrors>$(WarningsAsErrors);NU1608</WarningsAsErrors>`).
  The restore then refuses such a pairing. But it refuses one of a third-party package's ranges too.

## Building it, once answered

In this order, each step a commit of its own:

1. The manifest reader and the upgrade report's restore graph reader learn `[x.y.z]`.
2. Direct references to Compze.Sql.Common for the 24 packages; the removal of each grant the build shows unused; the code
   policy.
3. The pack target.
4. The checks of section 4, the check of the changelog line, and the dry run's listing.
5. `C-Prepare-Release`: the added packages, their bump and their line. `C-Get-PackagesChangedSinceTheirRelease`: how many
   packages each release would release.
6. The pending-release feed's `-Replace`.
7. The upgrade report's exact versions.
8. The skill releasing-compze-packages and the rule on releasing; the message to the tooling owner about the skill
   upgrading-compze-packages.

Verified by `C-Build` at 0 warnings, the full suite on MsSql, PgSql, MySql, Sqlite and SqliteMemory with
Microsoft:Newtonsoft:AspNetCore, and a `C-SimulateRelease` that shows the exact versions. Nothing awaits release at HEAD,
so that simulation runs in a throwaway worktree where `C-Prepare-Release` has prepared a patch of Compze.Sql.Sqlite, which
releases 8 packages.
