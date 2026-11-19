# Releasing: run the Create Releases workflow only after a CI run that covers the commit has passed

The Create Releases workflow (`.github/workflows/publish-nuget.yml`) publishes to nuget.org, and a published version can
never be replaced. It tests only three combinations itself: the CI workflow (`ci.yml`) is what tests the full
pluggable-component matrix. So a release waits for a CI run that covers its commit to pass, which the workflow's first
step, the release gate, enforces.

## Before preparing or making a release: load the skill `releasing-compze-packages`

It has the procedure, from `C-Prepare-Release` to Create Releases, and who carries it out. Load it before bumping a
package's version, before publishing to the pending-release feed, and before running Create Releases.

Create Releases publishes to an external service, so it runs only on Magnus's go-ahead, and one go-ahead covers one
release.
