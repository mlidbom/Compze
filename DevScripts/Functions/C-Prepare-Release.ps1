# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Prepare-Release {
    <#
    .SYNOPSIS
    Prepares the next release of every package whose changelog has gained entries since its last release

    .DESCRIPTION
    During development, new changelog entries go into a changelog's top section, which is the section of the version last
    released. Preparing a release splits that section. For each package (src/<PackageId>) whose version has a release
    tag, and whose top section has gained lines since that release:
    - Its <Version> is bumped: the minor version by default, the patch version with -Patch. The prerelease label stays.
    - A section for the new version goes above the released one, holding the lines the released section gained, in their
      order. Lines edited in place count as gained: they describe the code as it is now.
    - The released section is restored to the text it was released with, from the release tag.
    - Then everything is packed (C-Pack -CI), and the section of every version awaiting release, prepared now or before,
      gets the line the release requires when the version raises Compze dependency floors across a minor version
      (.github/scripts/lib/DependencyFloors.ps1 says why), or loses one that no longer holds. It is computed as the release
      computes it, against the package's previous version on nuget.org. A version prepared before its dependencies were
      gets its line then, so running the command again is safe.

    A package whose top section gained nothing isn't prepared: a change no changelog entry describes isn't one to release.
    Neither is a package without a release tag at its version: a new package, or one already prepared.

    So before changing anything, it refuses, as C-SimulateRelease and the release do, when a package it doesn't prepare,
    whose version has a release tag, has shipped files that changed since that tag: its project folder's files but its
    CHANGELOG.md and its _docs and dev_docs folders. The release would leave such a package at its released version, and a
    package released beside it that needs what changed would fail against it. The refusal names each package and its
    changed files. Either give the package a changelog entry, so that it is prepared, or, when its changes ship nothing a
    consumer sees, acknowledge them with C-Acknowledge-UnreleasedChanges, then run this again. Preparing a package's release
    removes its acknowledgement, which the release makes moot. With nothing to release, it reports that and refuses nothing.

    Returns what it prepared in this run: each package, and its version before and after. Review the changes with git diff, then run
    C-SimulateRelease, which checks them as the release will.

    .PARAMETER PackageId
    Prepare only these packages.

    .PARAMETER Patch
    Bump the patch version instead of the minor version: for releases whose changes no consumer meets.

    .PARAMETER PatchPackageId
    Bump the patch version of these packages, and the minor version of the others it prepares: for a release that mixes
    the two. Each must be one it prepares in this run; it refuses otherwise, changing nothing.

    .EXAMPLE
    C-Prepare-Release

    .EXAMPLE
    C-Prepare-Release -PackageId Compze.Tessaging -Patch

    .EXAMPLE
    C-Prepare-Release -PatchPackageId Compze.Must, Compze.Hosting
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string[]]$PackageId,
        [switch]$Patch,
        [string[]]$PatchPackageId = @()
    )

    $ErrorActionPreference = 'Stop'

    # Helpers, nested so that the module doesn't export them.
    function Get-PackagesWithAChangelog([string[]]$PackageId) {
        # The packable projects under src that have a changelog, limited to $PackageId when given, each with whether its
        # <Version> has a release tag (<PackageId>/v<Version>).
        $tags = @(git -C $script:CompzeRoot tag --list "*/v*")
        foreach ($project in Get-ChildItem -Path (Join-Path $script:CompzeRoot "src") -Directory) {
            if ($PackageId -and $PackageId -notcontains $project.Name) { continue }
            $projectFile = Join-Path $project.FullName "$($project.Name).csproj"
            $changelog = Join-Path $project.FullName "CHANGELOG.md"
            if (-not ((Test-Path -LiteralPath $projectFile) -and (Test-Path -LiteralPath $changelog))) { continue }
            $versionMatch = [regex]::Match((Read-TextFile $projectFile).Text, '<Version>([^<]+)</Version>')
            if (-not $versionMatch.Success) { continue }
            $version = $versionMatch.Groups[1].Value
            [PSCustomObject]@{
                PackageId     = $project.Name
                Version       = $version
                ProjectFile   = $projectFile
                Changelog     = $changelog
                IsReleased    = $tags -contains "$($project.Name)/v$version"
            }
        }
    }

    function Get-PackagesAwaitingRelease([string[]]$PackageId) {
        # The packages whose version has no release tag, and has its changelog section: prepared, now or before.
        Get-PackagesWithAChangelog $PackageId | Where-Object { -not $_.IsReleased -and (Get-TopChangelogSection (Read-TextFile $_.Changelog).Lines).Version -eq $_.Version }
    }

    function Get-ChangelogLinesGainedSinceTheRelease($Package) {
        # The lines of the changelog's top section that its text at the release tag doesn't have, in their order. The top
        # section must be the released version's.
        $current = Get-TopChangelogSection (Read-TextFile $Package.Changelog).Lines
        if ($current.Version -ne $Package.Version) {
            throw "C-Prepare-Release: the top section of src/$($Package.PackageId)/CHANGELOG.md is $($current.Version), but the package's version, which has a release tag, is $($Package.Version)."
        }
        $released = Get-ReleasedChangelogSection $Package
        $releasedLineCounts = @{}
        foreach ($line in $released.BodyLines) { $releasedLineCounts[$line] = 1 + [int]$releasedLineCounts[$line] }
        foreach ($line in $current.BodyLines) {
            if ([int]$releasedLineCounts[$line] -gt 0) { $releasedLineCounts[$line]--; continue }
            $line
        }
    }

    function Get-ReleasedChangelogSection($Package) {
        # The released version's section of the changelog, as it was at the release tag.
        $changelogAtTheTag = Read-CompzeFileAtRevision -Revision "$($Package.PackageId)/v$($Package.Version)" -Path "src/$($Package.PackageId)/CHANGELOG.md"
        if ($null -eq $changelogAtTheTag) { throw "C-Prepare-Release: can't read src/$($Package.PackageId)/CHANGELOG.md at the release tag $($Package.PackageId)/v$($Package.Version)." }
        $section = Get-ChangelogSectionsWithLines @($changelogAtTheTag | ForEach-Object { $_.TrimEnd("`r") }) | Where-Object { $_.Version -eq $Package.Version } | Select-Object -First 1
        if (-not $section) { throw "C-Prepare-Release: src/$($Package.PackageId)/CHANGELOG.md had no $($Package.Version) section at its release tag." }
        return $section
    }

    function Split-ReleasedChangelogSection($Package, [string]$NewVersion, [string[]]$GainedLines) {
        # Puts a section for the new version, holding the gained lines, above the top section, and restores the top section
        # to its released text.
        $file = Read-TextFile $Package.Changelog
        $top = Get-TopChangelogSection $file.Lines
        $released = Get-ReleasedChangelogSection $Package
        $newSection = @("## $NewVersion", "") + @($GainedLines | Where-Object { $_.Trim() }) + @("")
        $lines = @($file.Lines[0..($top.HeadingIndex - 1)]) + $newSection + @($file.Lines[$top.HeadingIndex]) + $released.BodyLines +
                 @(if ($top.EndIndex -lt $file.Lines.Count) { $file.Lines[$top.EndIndex..($file.Lines.Count - 1)] })
        Write-TextFile $file $lines
    }

    function Set-RaisedDependencyFloorsLine($Package, [string]$ExpectedLine) {
        # Makes the top section's line stating raised dependency floors the expected one, the section's last line: removes
        # the line when none is expected.
        $file = Read-TextFile $Package.Changelog
        $top = Get-TopChangelogSection $file.Lines
        $body = @($top.BodyLines | Where-Object { -not $_.StartsWith($script:RaisedDependencyFloorsLineLead) })
        $lastContentIndex = $body.Count - 1
        while ($lastContentIndex -ge 0 -and -not $body[$lastContentIndex].Trim()) { $lastContentIndex-- }
        $newBody = @(if ($lastContentIndex -ge 0) { $body[0..$lastContentIndex] }) + @(if ($ExpectedLine) { $ExpectedLine }) +
                   @(if ($lastContentIndex + 1 -lt $body.Count) { $body[($lastContentIndex + 1)..($body.Count - 1)] })
        if (($newBody -join "`n") -eq ($top.BodyLines -join "`n")) { return }
        $lines = @($file.Lines[0..$top.HeadingIndex]) + $newBody + @(if ($top.EndIndex -lt $file.Lines.Count) { $file.Lines[$top.EndIndex..($file.Lines.Count - 1)] })
        Write-TextFile $file $lines
    }

    function Get-TopChangelogSection([string[]]$Lines) {
        $section = Get-ChangelogSectionsWithLines $Lines | Select-Object -First 1
        if (-not $section) { throw "C-Prepare-Release: a changelog has no '## <version>' section." }
        return $section
    }

    function Get-ChangelogSectionsWithLines([string[]]$Lines) {
        # Each "## <version>" section: its version, the index of its heading, the index of the line after it ends, and the
        # lines between them. The version is the heading's first word, as in the release scripts' Changelogs.ps1.
        $headingIndexes = @(for ($index = 0; $index -lt $Lines.Count; $index++) { if ($Lines[$index] -match '^## ') { $index } })
        for ($position = 0; $position -lt $headingIndexes.Count; $position++) {
            $headingIndex = $headingIndexes[$position]
            $endIndex = if ($position + 1 -lt $headingIndexes.Count) { $headingIndexes[$position + 1] } else { $Lines.Count }
            [PSCustomObject]@{
                Version      = ($Lines[$headingIndex].Substring(3).Trim() -split '\s+')[0]
                HeadingIndex = $headingIndex
                EndIndex     = $endIndex
                BodyLines    = @(if ($endIndex - 1 -gt $headingIndex) { $Lines[($headingIndex + 1)..($endIndex - 1)] })
            }
        }
    }

    function Get-BumpedVersion([string]$Version, [switch]$Patch) {
        $match = [regex]::Match($Version, '^(\d+)\.(\d+)\.(\d+)(-.+)?$')
        if (-not $match.Success) { throw "C-Prepare-Release: can't bump the version '$Version': it isn't <major>.<minor>.<patch>[-<prerelease>]." }
        $major = [int]$match.Groups[1].Value
        $minor = [int]$match.Groups[2].Value
        $patchNumber = [int]$match.Groups[3].Value
        $prerelease = $match.Groups[4].Value
        if ($Patch) { return "$major.$minor.$($patchNumber + 1)$prerelease" }
        return "$major.$($minor + 1).0$prerelease"
    }

    function Set-ProjectVersion($Package, [string]$NewVersion) {
        $file = Read-TextFile $Package.ProjectFile
        $text = $file.Text.Replace("<Version>$($Package.Version)</Version>", "<Version>$NewVersion</Version>")
        Write-TextFile $file ($text -split "`n")
    }

    function Read-TextFile([string]$Path) {
        # The file's lines, its line ending and whether it starts with a byte order mark, so Write-TextFile writes it back the way it was.
        $bytes = [System.IO.File]::ReadAllBytes($Path)
        $hasByteOrderMark = $bytes.Length -ge 3 -and $bytes[0] -eq 0xEF -and $bytes[1] -eq 0xBB -and $bytes[2] -eq 0xBF
        $text = [System.Text.UTF8Encoding]::new($false).GetString($bytes, $(if ($hasByteOrderMark) { 3 } else { 0 }), $bytes.Length - $(if ($hasByteOrderMark) { 3 } else { 0 }))
        $lineEnding = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
        $normalized = $text.Replace("`r`n", "`n")
        return [PSCustomObject]@{ Path = $Path; Text = $normalized; Lines = @($normalized -split "`n"); LineEnding = $lineEnding; HasByteOrderMark = $hasByteOrderMark }
    }

    function Write-TextFile($File, [string[]]$Lines) {
        [System.IO.File]::WriteAllText($File.Path, ($Lines -join $File.LineEnding), [System.Text.UTF8Encoding]::new($File.HasByteOrderMark))
    }

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/DependencyFloors.ps1")
    . (Join-Path $script:CompzeRoot ".github/scripts/lib/PackageManifests.ps1")
    . (Join-Path $script:CompzeRoot ".github/scripts/lib/UnreleasedChanges.ps1")

    $fetchResult = git -C $script:CompzeRoot fetch --all --tags --quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not fetch the latest tags, so packages released elsewhere may be prepared again.`n$fetchResult"
    }

    $toPrepare = @(foreach ($package in @(Get-PackagesWithAChangelog $PackageId | Where-Object IsReleased)) {
        $gainedLines = @(Get-ChangelogLinesGainedSinceTheRelease $package)
        if ($gainedLines | Where-Object { $_.Trim() }) { [PSCustomObject]@{ Package = $package; GainedLines = $gainedLines } }
    })
    if ($toPrepare.Count -eq 0 -and @(Get-PackagesAwaitingRelease $PackageId).Count -eq 0) {
        Write-Host "Nothing to prepare: no released package's changelog has gained entries since its release."
        return
    }

    $notPrepared = @($PatchPackageId | Where-Object { @($toPrepare | ForEach-Object { $_.Package.PackageId }) -notcontains $_ })
    if ($notPrepared.Count -gt 0) { throw "C-Prepare-Release refuses, and prepared nothing: -PatchPackageId names packages it doesn't prepare in this run: $($notPrepared -join ', ')." }

    $unacknowledgedUnreleasedChanges = @(GetUnacknowledgedUnreleasedChanges -PackagesBeingPrepared @($toPrepare | ForEach-Object { $_.Package.PackageId }))
    if ($unacknowledgedUnreleasedChanges.Count -gt 0) { throw "C-Prepare-Release refuses, and prepared nothing: $(FormatUnreleasedChangesRefusal $unacknowledgedUnreleasedChanges)" }

    $prepared = @(foreach ($preparation in $toPrepare) {
        $newVersion = Get-BumpedVersion $preparation.Package.Version -Patch:($Patch -or $PatchPackageId -contains $preparation.Package.PackageId)
        Set-ProjectVersion $preparation.Package $newVersion
        Split-ReleasedChangelogSection $preparation.Package $newVersion $preparation.GainedLines
        [PSCustomObject]@{ PackageId = $preparation.Package.PackageId; From = $preparation.Package.Version; To = $newVersion }
    })
    if ($prepared.Count -gt 0) { RemoveUnreleasedChangeAcknowledgements @($prepared | ForEach-Object { $_.PackageId }) }
    $awaitingRelease = @(Get-PackagesAwaitingRelease $PackageId)

    C-Pack -CI
    if ($global:LASTEXITCODE -ne 0) { throw "C-Prepare-Release: packing failed (see above), so the dependency floors lines weren't checked." }

    $versionsOnNuGetOrg = Get-NuGetOrgRestorableVersions -PackageId @($awaitingRelease | ForEach-Object { $_.PackageId })
    foreach ($package in $awaitingRelease) {
        $previousVersion = @($versionsOnNuGetOrg[$package.PackageId] | Where-Object { [semver]$_ -lt [semver]$package.Version } | Sort-Object { [semver]$_ }) | Select-Object -Last 1
        if (-not $previousVersion) { continue }
        $nupkgPath = Join-Path $script:CompzeRoot "nupkgs/$($package.PackageId).$($package.Version).nupkg"
        Set-RaisedDependencyFloorsLine $package (FormatRaisedDependencyFloorsLine @(GetDependencyFloorsRaisedAcrossAMinorVersion (ReadNuGetOrgPackageManifest $package.PackageId $previousVersion) (ReadNupkgManifest $nupkgPath)))
    }

    $prepared
}
