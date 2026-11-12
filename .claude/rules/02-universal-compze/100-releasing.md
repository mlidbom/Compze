# Releasing: run the Create Releases workflow only after CI has passed on the same commit

The Create Releases workflow (`.github/workflows/publish-nuget.yml`) publishes to nuget.org, but it tests only the
combinations in `src/TestUsingPluggableComponentCombinations.github-ci`: three, all SqliteMemory, on Linux. The CI
workflow (`ci.yml`), which runs on every push, is what tests the full pluggable-component matrix: every persistence
layer, DI container and transport on Linux (40 combinations), and the Sqlite ones on Windows (16 more). Published
before CI has passed, a release can ship packages that fail on every combination the workflow didn't test, and a
published version can never be replaced.

The workflow enforces this itself: its first step, the release gate (`.github/scripts/Assert-CiPassedForCommit.ps1`),
stops the release unless a CI run of the commit ran its test jobs and every one of its jobs succeeded. A run's
conclusion alone proves nothing: a CI run for a commit CI already ran under another branch skips its test jobs and still
concludes `success`.

A release goes in this order:

1. **Prepare it:** bump the versions, write the changelog sections, and run `C-SimulateRelease`. It refuses a section
   that doesn't state the Compze dependencies whose minimum version the release raises across a minor version, which
   a consumer's upgrade brings along, and prints the exact line to add (`.github/scripts/lib/DependencyFloors.ps1`
   says why). Each section also becomes its package's release notes on nuget.org.
2. **Commit and push.**
3. **Wait for CI to pass on that commit:** `gh run watch <run id>`, the id from `gh run list --workflow ci.yml --commit <sha>`.
   Run the gate yourself to check, as the workflow will:
   `& .github/scripts/Assert-CiPassedForCommit.ps1 -Repository mlidbom/Compze -CommitSha <full sha>`.
4. **Then run Create Releases,** by hand: `gh workflow run publish-nuget.yml --ref main`.
   - It runs on the branch's head, not on a commit you pick. If anything was pushed since step 3, its CI must pass too,
     or the gate stops the release.
   - It publishes to an external service, so it runs only on Magnus's explicit go-ahead: see below.

## Who carries it out: you, on one go-ahead per release

Magnus usually wants the agent that prepared a release to carry it through, not hand him steps 2–4 (Magnus,
2026-11-12). So once a release is prepared, ask him once: *push, wait for CI, and run Create Releases?*

- **A yes covers steps 2–4 for that release, and only that one.** The next release needs its own yes: publishing
  can't be undone.
- **Then carry it through without stopping:** push, watch the CI run to its end, run the release gate, run Create
  Releases and watch it until it has published.
- **If CI fails, don't release, and don't rerun CI hoping for green.** A failure is a finding, never noise: see the
  rule on diagnosing CI flakiness. Report it to Magnus.
