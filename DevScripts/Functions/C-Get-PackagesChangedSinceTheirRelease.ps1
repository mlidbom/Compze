# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-PackagesChangedSinceTheirRelease {
    <#
    .SYNOPSIS
    Lists the packages whose shipped files changed since their last release tag, for checking a release's changelogs

    .DESCRIPTION
    For each packable project under src, whose <Version> is published to nuget.org: its last release tag merged into
    HEAD (<PackageId>/v<Version>, the most recently created), and the files of its project folder that changed between
    that tag and HEAD, leaving out its CHANGELOG.md and its _docs and dev_docs folders, which the package doesn't ship.

    Lists only the packages with such changes, and those never released. For each it says whether the version has been
    bumped since the release, which C-Prepare-Release does, and whether the changelog changed since the tag: a package
    that changed without either needs a changelog entry, and a version bump through C-Prepare-Release. Whether the
    entries cover the changes is for a reader of the diff to judge: git diff <ReleaseTag> HEAD -- src/<PackageId>.

    .PARAMETER PackageId
    Limits the list to these packages.

    .OUTPUTS
    One object per package: PackageId, ReleaseTag (null when never released), Version, IsBumped, ChangelogChanged and
    ChangedFiles, the paths relative to the repository root.

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

    foreach ($project in Get-ChildItem -Path $script:CompzeSrcRoot -Directory) {
        if ($PackageId -and $PackageId -notcontains $project.Name) { continue }
        $projectFile = Join-Path $project.FullName "$($project.Name).csproj"
        if (-not (Test-Path -LiteralPath $projectFile)) { continue }
        $versionMatch = [regex]::Match((Get-Content -LiteralPath $projectFile -Raw), '<Version>([^<]+)</Version>')
        if (-not $versionMatch.Success) { continue }
        $version = $versionMatch.Groups[1].Value
        if ($version -eq '0.0.0-not-published-to-nuget') { continue }

        $projectPath = "src/$($project.Name)"
        $releaseTag = @(git -C $script:CompzeRoot tag --list "$($project.Name)/v*" --merged HEAD --sort=-creatordate) | Select-Object -First 1
        $changedFiles = if ($releaseTag) {
            @(git -C $script:CompzeRoot diff --name-only $releaseTag HEAD -- $projectPath)
        } else {
            @(git -C $script:CompzeRoot ls-files -- $projectPath)
        }
        $shippedFiles = @($changedFiles | Where-Object { $_ -ne "$projectPath/CHANGELOG.md" -and $_ -notmatch '(^|/)(_docs|dev_docs)/' })
        if ($releaseTag -and $shippedFiles.Count -eq 0) { continue }

        $changelogChanged = if ($releaseTag) { $changedFiles -contains "$projectPath/CHANGELOG.md" } else { Test-Path -LiteralPath (Join-Path $project.FullName 'CHANGELOG.md') }
        [PSCustomObject]@{
            PackageId        = $project.Name
            ReleaseTag       = $releaseTag
            Version          = $version
            IsBumped         = -not $releaseTag -or $releaseTag -ne "$($project.Name)/v$version"
            ChangelogChanged = $changelogChanged
            ChangedFiles     = $shippedFiles
        }
    }
}
