# The release gate: fails unless the CI workflow (ci.yml) has fully tested what the commit about to be released has.
#
# The Create Releases workflow tests only the combinations in src/TestUsingPluggableComponentCombinations.github-ci;
# CI is what runs the full pluggable-component matrix, on Linux and Windows. The gate passes when a CI run that covers
# the commit has passed: a run of the commit itself, or of an earlier commit that differs from it only in files CI doesn't
# test, such as documentation (lib/CiCoverage.ps1 says which). A run has passed only when it ran its test jobs and every
# one of its jobs succeeded. A run of a commit that an earlier run covers skips its test jobs and still concludes success,
# so a run's conclusion alone proves nothing.
#
# Used by the Create Releases workflow (publish-nuget.yml) as its first step. See
# .claude/rules/02-universal-compze/100-releasing.md for the release procedure.
#
# Exits 0 when a CI run that covers the commit has passed, 1 otherwise, saying why. Needs gh, with read access to the
# repository's Actions (GH_TOKEN in a workflow), and a clone of the repository that has the commit with its history.
#
# Usage:
#   & .github/scripts/Assert-CiPassedForCommit.ps1 -Repository mlidbom/Compze -CommitSha <commit>

param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$CommitSha
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

. "$PSScriptRoot/lib/CiCoverage.ps1"

$commitsTheSameToCi = @(GetCommitsTheSameToCiAs $CommitSha)
$theCommit = $commitsTheSameToCi[0]

function Describe([string]$Commit) {
    if ($Commit -eq $theCommit) { return "commit $theCommit" }
    return "commit $Commit, which is the same to CI as commit $theCommit"
}

$whyEachRunHasNotPassed = [System.Collections.Generic.List[string]]::new()
foreach ($commit in $commitsTheSameToCi) {
    foreach ($run in GetCiRunsOf $Repository $commit) {
        $jobs = @(GetJobsOf $Repository $run)
        $whyNot = WhyTheRunHasNotPassed $run $jobs
        if ($null -eq $whyNot) {
            $testJobCount = @($jobs | Where-Object { IsTestJob $_ }).Count
            Write-Host "CI passed on $(Describe $commit): all $($jobs.Count) jobs of $($run.Url) succeeded, $testJobCount of them test jobs."
            exit 0
        }
        $whyEachRunHasNotPassed.Add("  $($run.Url) ($($run.Branch)), of $(Describe $commit): $whyNot")
    }
}

if ($whyEachRunHasNotPassed.Count -eq 0) {
    Write-Host "No CI run covers commit $theCommit. Push it, wait for CI to pass, and release then."
    exit 1
}

Write-Host "No CI run that covers commit $theCommit has passed, so this release stops here. The CI runs that cover it:"
$whyEachRunHasNotPassed | ForEach-Object { Write-Host $_ }
exit 1
