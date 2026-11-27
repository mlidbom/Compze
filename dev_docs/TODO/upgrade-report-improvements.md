# C-Get-CompzeUpgradeReport: improvements proposed, not yet made

Found by the helper that added the report's left-behind section (2026-11-27). Not decided.

- **A one-line answer for most consumers.** SystemCE comes along with 5 of the tooling's 9 upgrades, so the same
  left-behind packages repeat from section to section. A line such as "upgrading every Compze package to its newest
  version gives only pairs the latest release checked" would be the answer for most consumers.
- **A `-CheckBindings` switch.** It would run the post-release binding check on the solution's package set as it would
  be after the upgrade, answering "will it bind" outright, where the left-behind section infers it from the
  `InternalsVisibleTo` grants. It costs downloads and a `dotnet run`.
- **RetiredNuGetPackageIds.txt is incomplete.** It lacks Compze.SystemCE.Internal, Compze.Threading.Internal and the
  Compze.Utilities.* ids. The report no longer relies on the file. Adding them is Magnus's call, since
  C-Unlist-RetiredNuGetPackages unlists on nuget.org whatever the file names.
- **The different-versions section's false negative** is marked with a `bug:` comment in the command itself.
