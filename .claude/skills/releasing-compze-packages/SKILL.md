---
name: releasing-compze-packages
description: >-
  How a release of Compze's packages to nuget.org is made: preparing it with C-Prepare-Release (version bumps,
  changelog sections, dependency floors), checking it with C-SimulateRelease, publishing the versions to the
  pending-release feed, waiting for a CI run that covers the commit, the release gate, and the Create Releases
  workflow. Use it before preparing or making any release: when Magnus asks to release, publish or ship a package or
  a change, when a consumer such as Deskmancer or JAStudio waits for a Compze change, and before bumping a package's
  version or running Create Releases.
---

# Releasing Compze's packages: prepare, push, wait for CI, then run Create Releases

The Create Releases workflow (`.github/workflows/publish-nuget.yml`) publishes to nuget.org, but it tests only the
combinations in `src/TestUsingPluggableComponentCombinations.github-ci`: three, all SqliteMemory, on Linux. The CI
workflow (`ci.yml`), which runs on every push, is what tests the full pluggable-component matrix: every persistence
layer, DI container and transport on Linux (40 combinations), and the Sqlite ones on Windows (16 more). Published
before CI has passed, a release can ship packages that fail on every combination the workflow didn't test, and a
published version can never be replaced.

The workflow enforces this itself: its first step, the release gate (`.github/scripts/Assert-CiPassedForCommit.ps1`),
stops the release unless a CI run that covers the commit ran its test jobs and every one of its jobs succeeded.

- **A CI run covers a commit** when it is a run of that commit, or of an earlier commit that differs from it only in
  files CI doesn't test: documentation, the DevScripts commands, and the rules and settings of development tools.
  `.github/scripts/lib/CiCoverage.ps1` lists them.
- **CI skips its test jobs for a commit that an earlier run covers,** when that run has passed or is still running. So a
  push that changes only such files starts no tests, and neither does a commit pushed under a second branch.
- **A run's conclusion alone proves nothing:** a run that skipped its test jobs still concludes `success`. Its summary
  names the run that covers the commit.

A release goes in this order:

1. **Prepare it:** `C-Prepare-Release` does it for every package whose changelog gained entries since its release:
   - it bumps the versions, minor by default, patch with `-Patch`, and only the packages named with `-PackageId`;
   - it splits each changelog's top section into the new version's section and the released one, restored from its tag;
   - it adds the line stating the Compze dependencies whose minimum version the release raises across a minor version,
     which a consumer's upgrade brings along (`.github/scripts/lib/DependencyFloors.ps1` says why).

   Then run `C-SimulateRelease`, which checks it all as the release will. Each section also becomes its package's
   release notes on nuget.org.
2. **Commit and push.**
3. **Publish the versions to the pending-release feed,** while CI runs: `C-Publish-ToPendingReleaseFeed`. The other
   repositories on Magnus's machine, such as Deskmancer, can then use them at once, instead of waiting half an hour to
   over an hour for CI, Create Releases and nuget.org's validation. See the user-level rule on local-feed cache
   poisoning for how a consumer uses the feed.
   - It must run before Create Releases: it publishes only versions without a release tag, and Create Releases tags them.
   - It changes only this machine, so the go-ahead for the release covers it.
4. **Wait for a CI run that covers that commit to pass:** `gh run watch <run id>`, the id from
   `gh run list --workflow ci.yml --commit <sha>`. If that run skipped its test jobs, watch the run its summary names.
   Run the gate yourself to check, as the workflow will:
   `& .github/scripts/Assert-CiPassedForCommit.ps1 -Repository mlidbom/Compze -CommitSha <sha>`.
5. **Then run Create Releases,** by hand: `gh workflow run publish-nuget.yml --ref main`.
   - It runs on the branch's head, not on a commit you pick. If anything CI tests was pushed since step 4, CI must pass
     on that too, or the gate stops the release.
   - It publishes to an external service, so it runs only on Magnus's explicit go-ahead: see below.

## Who carries it out: you, on one go-ahead per release

Magnus usually wants the agent that prepared a release to carry it through, not hand him steps 2–5 (Magnus,
2026-11-12). So once a release is prepared, ask him once: *push, wait for CI, and run Create Releases?*

- **A yes covers steps 2–5 for that release, and only that one.** The next release needs its own yes: publishing
  can't be undone.
- **Then carry it through without stopping:** push, publish to the pending-release feed, watch the CI run to its end,
  run the release gate, run Create Releases and watch it until it has published.
- **If CI fails, don't release, and don't rerun CI hoping for green.** A failure is a finding, never noise: see the
  rule on diagnosing CI flakiness. Report it to Magnus.
