# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-PackagesChangedSinceTheirRelease {
    <#
    .SYNOPSIS
    Lists the packages whose shipped files changed since their last release tag, for checking a release's changelogs

    .DESCRIPTION
    For each packable project under src, whose <Version> is published to nuget.org: its last release tag merged into
    HEAD (<PackageId>/v<Version>, the most recently created), and the files of its project folder that changed between
    that tag and HEAD, leaving out its CHANGELOG.md and its _docs and dev_docs folders, which the package doesn't ship.
    Changes acknowledged in .github/scripts/AcknowledgedUnreleasedChanges.txt, as shipping nothing a consumer sees, need no
    changelog entry: for a package named there, only the files that changed after the commit it names count, when that
    commit comes after the tag.

    Lists only the packages with such changes, and those never released. For each it says whether the version has been
    bumped since the release, which C-Prepare-Release does, and whether the changelog changed since the tag: a package
    that changed without either needs a changelog entry, and a version bump through C-Prepare-Release. Whether the
    entries cover the changes is for a reader of the diff to judge: git diff <ReleaseTag> HEAD -- src/<PackageId>.

    .PARAMETER PackageId
    Limits the list to these packages.

    .OUTPUTS
    One object per package: PackageId, ReleaseTag (null when never released), Version, IsBumped, ChangelogChanged,
    ChangedSince (the release tag, or the commit the package's changes are acknowledged up to) and ChangedFiles, the paths
    relative to the repository root.

    .EXAMPLE
    C-Get-PackagesChangedSinceTheirRelease | Format-Table PackageId, ReleaseTag, Version, IsBumped, ChangelogChanged, @{ n = 'Files'; e = { $_.ChangedFiles.Count } }

    .EXAMPLE
    (C-Get-PackagesChangedSinceTheirRelease -PackageId Compze.Sql.Common).ChangedFiles
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string[]]$PackageId
    )

    $ErrorActionPreference = 'Stop'

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/UnreleasedChanges.ps1")
    $acknowledgements = ReadUnreleasedChangeAcknowledgements

    foreach ($package in GetPublishablePackagesUnderSrc) {
        if ($PackageId -and $PackageId -notcontains $package.PackageId) { continue }

        $releaseTag = @(git -C $script:CompzeRoot tag --list "$($package.PackageId)/v*" --merged HEAD --sort=-creatordate) | Select-Object -First 1
        $changedSince = if ($releaseTag) { GetUnreleasedChangesStart $releaseTag $acknowledgements[$package.PackageId] } else { $null }
        $shippedFiles = @(if ($releaseTag) {
            GetShippedFilesChangedSince $package $changedSince
        } else {
            SelectShippedFiles $package @(git -C $script:CompzeRoot -c core.quotePath=false ls-files -- $package.ProjectFolder)
        })
        if ($releaseTag -and $shippedFiles.Count -eq 0) { continue }

        $changelog = "$($package.ProjectFolder)/CHANGELOG.md"
        $changelogChanged = if ($releaseTag) { [bool](git -C $script:CompzeRoot diff --name-only $releaseTag HEAD -- $changelog) } else { Test-Path -LiteralPath (Join-Path $script:CompzeRoot $changelog) }
        [PSCustomObject]@{
            PackageId        = $package.PackageId
            ReleaseTag       = $releaseTag
            Version          = $package.Version
            IsBumped         = -not $releaseTag -or $releaseTag -ne "$($package.PackageId)/v$($package.Version)"
            ChangelogChanged = $changelogChanged
            ChangedSince     = $changedSince
            ChangedFiles     = $shippedFiles
        }
    }
}
