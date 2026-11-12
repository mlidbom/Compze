# The post-release binding check

Fails a release when, once it is out, the Compze packages nuget.org serves would fail at runtime against each other.

## Why it exists

A release publishes only the packages that changed. A consumer that upgrades them runs them together with the other Compze
packages exactly as nuget.org has them. Nothing in Compze's own build runs that mix: the solution builds every package
against the source of the others. So two kinds of break go unnoticed until a consumer meets them at runtime, as a
`TypeLoadException`, `MissingMethodException` or `MemberAccessException`:

- **A released package uses what the release removes or changes.** Compze.Internals.Logging 0.5.0-alpha removed the
  `Logger` class, which the released Compze.Internals.Logging.Serilog 0.4.1-alpha extends.
- **A package of the release relies on a change in another package that was never released**, because that package's
  version wasn't bumped.

Compze's dependency versions have no upper bound (`version="0.4.1-alpha"` means 0.4.1-alpha or newer), so NuGet
resolves either mix without a word.

## What it checks

The *post-release package set*: for each Compze package, the newest version a consumer gets once the release is out.
That's the release's own build of each package it publishes, and nuget.org's newest version of every other package.
Every assembly in the set is checked against the other packages of the set:

- **References** (`Checks/ReferenceCheck.cs`): every type, method and field it uses in another package exists there, with
  the signature it was compiled against, and is accessible to it (public, protected, or internal with
  `InternalsVisibleTo` granted to it, as Compze's libraries grant each other). The references are resolved as the runtime
  binds them, through type forwarders and base classes, so moving a member to a base class is fine.
- **Type loading** (`Checks/TypeLoadCheck.cs`): no class it derives from in another package is sealed, and each of its
  concrete types implements every abstract member of the interfaces and base classes it inherits from other packages,
  including members added after it was compiled.

## What it doesn't check

- **Behavior.** That a reference still resolves doesn't mean it still does the same thing. Logging 0.5.0-alpha also
  changed how a plain-text message reaches the backend; only tests catch that.
- **Older versions.** Only the newest version of each package is checked: what a consumer that upgrades gets. A consumer
  pinned to an older release of one package can still break.
- **What is inherited through types outside the set**, such as `IDisposable`'s members, or a base class from another
  library. The check sees only the Compze packages.

## Where it runs

`Invoke-PostReleaseBindingCheck.ps1` downloads nuget.org's builds and runs this tool. It runs before anything is
published:

- in `Publish-NuGetWithReleases.ps1`, so in the Create Releases workflow and in `C-SimulateRelease`;
- in `C-Publish-ToPendingReleaseFeed`, since a consumer on this machine would get the same failures.

To run it by hand: `dotnet run --project .github/scripts/PostReleaseBindingCheck -- --this-release <nupkg>... --on-nuget-org <nupkg>...`.
It is silent when the set binds, and otherwise prints each failure, grouped by the pair of packages it is between.

## When it fails

The failure names the type or member and the two packages. What to do depends on the case:

- **Release the package that would fail as well**, built against the new code: bump its version and add a changelog
  entry. The usual fix.
- **Keep what the release removed**, when consumers are likely to stay on the released package.
- For a package of the release that relies on an unreleased change: **release the package it relies on**.

## Tests

`../PostReleaseBindingCheck.Specifications` compiles small libraries in two versions, packs them into nupkgs, and
checks the set they make.
