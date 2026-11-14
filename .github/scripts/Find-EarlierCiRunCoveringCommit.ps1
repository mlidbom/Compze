# Tells the CI workflow (ci.yml) whether to test the commit it was started for: it does unless an earlier CI run covers the
# commit, and has passed or is still running.
#
# A CI run covers a commit when it is a run of that commit, or of a commit that differs from it only in files CI doesn't
# test, such as documentation: lib/CiCoverage.ps1 says which files, and why its verdict then holds for both. So a push that
# changes nothing CI tests starts no test jobs, and neither does a commit that CI already runs under another branch.
#
# A covering run that failed doesn't count: the commit is then tested again, as it was before CI skipped anything.
# A covering run that is still running does count, although it may yet fail. The release gate
# (Assert-CiPassedForCommit.ps1) is what requires that it passed.
#
# Only earlier runs are looked at, those with a lower id than this run's. Of all the runs that cover one another, the
# earliest then finds none and tests, so some run always does. The one race is an earlier run that the API doesn't list
# yet, in its first seconds: that costs a run too many, never one too few.
#
# Writes found=true or found=false to the step's outputs (GITHUB_OUTPUT), and what it found to the run's summary
# (GITHUB_STEP_SUMMARY), when it runs in a workflow.
#
# Usage:
#   & .github/scripts/Find-EarlierCiRunCoveringCommit.ps1 -Repository mlidbom/Compze -CommitSha <commit> -RunId <this run's id>

param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$CommitSha,
    [Parameter(Mandatory)][long]$RunId
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

. "$PSScriptRoot/lib/CiCoverage.ps1"

$commitsTheSameToCi = @(GetCommitsTheSameToCiAs $CommitSha)
$theCommit = $commitsTheSameToCi[0]

function HasPassedOrIsStillRunning($Run) {
    if (IsStillRunning $Run) { return $true }
    return $null -eq (WhyTheRunHasNotPassed $Run @(GetJobsOf $Repository $Run))
}

function FindEarlierRunThatCoversTheCommit {
    foreach ($commit in $commitsTheSameToCi) {
        $earlierRuns = @(GetCiRunsOf $Repository $commit | Where-Object { $_.Id -lt $RunId } | Sort-Object Id)
        foreach ($run in $earlierRuns) {
            if (HasPassedOrIsStillRunning $run) { return $run }
        }
    }
    return $null
}

function Describe($Run) {
    $howItWent = if (IsStillRunning $Run) { "is still running" } else { "passed" }
    $whatItCovers = if ($Run.Commit -eq $theCommit) { "An earlier CI run of commit $theCommit" }
                    else { "Commit $theCommit is the same to CI as commit $($Run.Commit), and an earlier CI run of that commit" }
    return "$whatItCovers $($howItWent): $($Run.Url). The commit is not tested again."
}

function Tell([string]$What) {
    Write-Host $What
    if ($env:GITHUB_STEP_SUMMARY) { $What >> $env:GITHUB_STEP_SUMMARY }
}

$earlierRun = FindEarlierRunThatCoversTheCommit
$found = $null -ne $earlierRun

if ($found) { Tell (Describe $earlierRun) }
else { Tell "No earlier CI run that passed or is still running covers commit ${theCommit}: it is tested now." }

if ($env:GITHUB_OUTPUT) { "found=$("$found".ToLowerInvariant())" >> $env:GITHUB_OUTPUT }
