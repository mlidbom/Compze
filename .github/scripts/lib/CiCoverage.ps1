# Which CI runs cover a commit.
#
# The CI workflow (ci.yml) builds the solution and runs its tests. Neither reads the files in $FilesCiDoesNotTest:
# documentation, the DevScripts commands, and the rules and settings of development tools. So two commits that differ only
# in such files are the same to CI: its verdict on one holds for the other. A CI run covers a commit when it is a run of
# that commit, or of a commit that is the same to CI.
#
# Two things rest on this:
#   - The CI workflow tests a commit only when no earlier CI run covers it (Find-EarlierCiRunCoveringCommit.ps1).
#   - The release gate accepts a commit when a CI run that covers it has passed (Assert-CiPassedForCommit.ps1).
#
# The functions read the clone of the repository that this file is in. It must have the commits asked about, with their
# history. They need gh, with read access to the repository's Actions (GH_TOKEN in a workflow).

# Wildcard patterns, matched with -like against a file's path from the root of the repository, so * also matches across
# folders. A file belongs here only when it is certain that neither the build of Compze.AllProjects.slnx nor its tests
# read it. Every other file is tested, whatever it is.
$FilesCiDoesNotTest = @(
    '*.md'               # Documentation. A package's README.md and CHANGELOG.md are read when it is packed, and CI packs nothing.
    '.claude/*'          # Claude's rules, skills and settings.
    'DevScripts/*.ps1'   # The DevScripts commands, none of which CI runs. The project file beside them is in the solution, so it is tested.
    'DevScripts/*.psm1'
    '.idea/*'            # The settings of editors and other development tools.
    '.vscode/*'
    '.serena/*'
    '.mcp.json'
    '.royder.json'       # Which solution Royder, the code-analysis MCP server, opens for this repository's files.
    '.github/scripts/AcknowledgedUnreleasedChanges.txt'  # Read only by the release scripts, never by the build or the tests.
    '*.DotSettings'
    '*.ncrunchsolution'
    '*.ncrunchproject'
)

# What covers a commit is nearly always the commit before it, or one a few commits before. So only a commit's ancestors are
# looked at, and of them only this many of the nearest.
$AncestorsLookedAtForTheSameToCi = 100

$RepositoryRoot = git -C $PSScriptRoot rev-parse --show-toplevel

function IsTestedByCi([string]$File) {
    foreach ($pattern in $FilesCiDoesNotTest) {
        if ($File -like $pattern) { return $false }
    }
    return $true
}

function FilesThatDifferBetween([string]$Commit, [string]$OtherCommit) {
    # --no-renames: finding renames compares the contents of files, which a clone made without them would have to download.
    # The CI workflow's clone is one.
    return @(git -C $RepositoryRoot -c core.quotePath=false diff --name-only --no-renames $Commit $OtherCommit)
}

function AreTheSameToCi([string]$Commit, [string]$OtherCommit) {
    $testedFilesThatDiffer = @(FilesThatDifferBetween $Commit $OtherCommit | Where-Object { IsTestedByCi $_ })
    return $testedFilesThatDiffer.Count -eq 0
}

# The commit itself, as its full id, then those of its nearest ancestors that are the same to CI as it, nearest first.
function GetCommitsTheSameToCiAs([string]$Commit) {
    $theCommitThenItsNearestAncestors = @(git -C $RepositoryRoot rev-list --max-count=$(1 + $AncestorsLookedAtForTheSameToCi) $Commit)
    return @($theCommitThenItsNearestAncestors | Where-Object { AreTheSameToCi $_ $Commit })
}

function GetCiRunsOf([string]$Repository, [string]$Commit) {
    $lines = @(gh api "repos/$Repository/actions/workflows/ci.yml/runs?head_sha=$Commit&per_page=100" `
                   --jq '.workflow_runs[] | [.id, .status, (.conclusion // ""), (.head_branch // ""), .html_url] | @tsv')
    foreach ($line in $lines) {
        $fields = $line -split "`t"
        [PSCustomObject]@{ Id = [long]$fields[0]; Commit = $Commit; Status = $fields[1]; Conclusion = $fields[2]; Branch = $fields[3]; Url = $fields[4] }
    }
}

function GetJobsOf([string]$Repository, $Run) {
    $lines = @(gh api "repos/$Repository/actions/runs/$($Run.Id)/jobs?per_page=100" --paginate `
                   --jq '.jobs[] | [.name, (.conclusion // "")] | @tsv')
    foreach ($line in $lines) {
        $fields = $line -split "`t"
        [PSCustomObject]@{ Name = $fields[0]; Conclusion = $fields[1] }
    }
}

function IsTestJob($Job) { $Job.Name -like 'Test (*' -or $Job.Name -like 'Windows Test (*' }

function IsStillRunning($Run) { $Run.Status -ne 'completed' }

# $null when the run has passed: it ran its test jobs, and every one of its jobs succeeded. Otherwise why it hasn't.
# A run's conclusion alone doesn't tell: a run that an earlier run covered skips its test jobs and still concludes success.
function WhyTheRunHasNotPassed($Run, $Jobs) {
    if (IsStillRunning $Run) { return "still running" }
    $testJobsThatRan = @($Jobs | Where-Object { (IsTestJob $_) -and $_.Conclusion -ne 'skipped' })
    if ($testJobsThatRan.Count -eq 0) {
        return "skipped its test jobs, as CI does when an earlier CI run covers the commit"
    }
    $jobsThatDidNotSucceed = @($Jobs | Where-Object { $_.Conclusion -ne 'success' })
    if ($jobsThatDidNotSucceed.Count -gt 0) {
        $listed = ($jobsThatDidNotSucceed | ForEach-Object { "$($_.Name): $($_.Conclusion)" }) -join "; "
        return "$($jobsThatDidNotSucceed.Count) of its $($Jobs.Count) jobs did not succeed: $listed"
    }
    return $null
}
