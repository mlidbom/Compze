# The release gate: fails unless the CI workflow (ci.yml) has fully tested the commit about to be released.
#
# The Create Releases workflow tests only the combinations in src/TestUsingPluggableComponentCombinations.github-ci;
# CI is what runs the full pluggable-component matrix, on Linux and Windows. A CI run counts as having tested the
# commit only when it has test jobs and every one of its jobs succeeded. A run for a commit CI already ran under another
# branch skips its test jobs and still concludes success, so a run's conclusion alone proves nothing.
#
# Used by the Create Releases workflow (publish-nuget.yml) as its first step. See
# .claude/rules/02-universal-compze/100-releasing.md for the release procedure.
#
# Exits 0 when a CI run of the commit passes, 1 otherwise, saying why. Needs gh, with read access to the repository's
# Actions (GH_TOKEN in a workflow).
#
# Usage:
#   & .github/scripts/Assert-CiPassedForCommit.ps1 -Repository mlidbom/Compze -CommitSha <full commit sha>

param(
    [Parameter(Mandatory)][string]$Repository,
    [Parameter(Mandatory)][string]$CommitSha
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

function GetCiRunsOfTheCommit {
    $lines = @(gh api "repos/$Repository/actions/workflows/ci.yml/runs?head_sha=$CommitSha&per_page=100" `
                   --jq '.workflow_runs[] | [.id, .status, (.conclusion // ""), .head_branch, .html_url] | @tsv')
    foreach ($line in $lines) {
        $fields = $line -split "`t"
        [PSCustomObject]@{ Id = $fields[0]; Status = $fields[1]; Conclusion = $fields[2]; Branch = $fields[3]; Url = $fields[4] }
    }
}

function GetJobs($Run) {
    $lines = @(gh api "repos/$Repository/actions/runs/$($Run.Id)/jobs?per_page=100" --paginate `
                   --jq '.jobs[] | [.name, (.conclusion // "")] | @tsv')
    foreach ($line in $lines) {
        $fields = $line -split "`t"
        [PSCustomObject]@{ Name = $fields[0]; Conclusion = $fields[1] }
    }
}

function IsTestJob($Job) { $Job.Name -like 'Test (*' -or $Job.Name -like 'Windows Test (*' }

function WhyTheRunDoesNotCount($Run, $Jobs) {
    if ($Run.Status -ne 'completed') { return "still running" }
    $testJobsThatRan = @($Jobs | Where-Object { (IsTestJob $_) -and $_.Conclusion -ne 'skipped' })
    if ($testJobsThatRan.Count -eq 0) {
        return "skipped its test jobs, as CI does when it already ran this commit under another branch"
    }
    $jobsThatDidNotSucceed = @($Jobs | Where-Object { $_.Conclusion -ne 'success' })
    if ($jobsThatDidNotSucceed.Count -gt 0) {
        $listed = ($jobsThatDidNotSucceed | ForEach-Object { "$($_.Name): $($_.Conclusion)" }) -join "; "
        return "$($jobsThatDidNotSucceed.Count) of its $($Jobs.Count) jobs did not succeed: $listed"
    }
    return $null
}

$runs = @(GetCiRunsOfTheCommit)
if ($runs.Count -eq 0) {
    Write-Host "CI has no run of commit $CommitSha. Push it, wait for CI to pass, and release then."
    exit 1
}

$whyEachRunDoesNotCount = [System.Collections.Generic.List[string]]::new()
foreach ($run in $runs) {
    $jobs = @(GetJobs $run)
    $whyNot = WhyTheRunDoesNotCount $run $jobs
    if ($null -eq $whyNot) {
        $testJobCount = @($jobs | Where-Object { IsTestJob $_ }).Count
        Write-Host "CI passed on commit ${CommitSha}: all $($jobs.Count) jobs of $($run.Url) succeeded, $testJobCount of them test jobs."
        exit 0
    }
    $whyEachRunDoesNotCount.Add("  $($run.Url) ($($run.Branch)): $whyNot")
}

Write-Host "CI has not passed on commit ${CommitSha}, so this release stops here. Its CI runs:"
$whyEachRunDoesNotCount | ForEach-Object { Write-Host $_ }
exit 1
