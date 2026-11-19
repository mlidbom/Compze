# Unreleased changes: changes to a package's shipped files since the release of the version it is at. A release that
# doesn't release the package leaves them behind: consumers keep the released version, and a package released beside it
# that needs what changed fails against it. So C-Prepare-Release, C-SimulateRelease and the Create Releases workflow
# (Publish-NuGetWithReleases.ps1) refuse while a package has unreleased changes, unless the changes are acknowledged:
# recorded in AcknowledgedUnreleasedChanges.txt as shipping nothing a consumer sees, such as comments only.
#
# A package's shipped files are those of its project folder, src/<PackageId>, but for its CHANGELOG.md and its _docs and
# dev_docs folders, which its package doesn't ship.
#
# The functions read the clone of the repository that this file is in. DevScripts dot-sources this file into an
# interactive PowerShell session, so nothing in it may call exit.

$RepositoryRoot = git -C $PSScriptRoot rev-parse --show-toplevel
$AcknowledgedUnreleasedChangesFile = '.github/scripts/AcknowledgedUnreleasedChanges.txt'

function GetPublishablePackagesUnderSrc {
    # Each packable project under src whose <Version> is published to nuget.org: its PackageId, its Version and its
    # ProjectFolder, src/<PackageId>.
    foreach ($project in Get-ChildItem -Path (Join-Path $RepositoryRoot 'src') -Directory) {
        $projectFile = Join-Path $project.FullName "$($project.Name).csproj"
        if (-not (Test-Path -LiteralPath $projectFile)) { continue }
        $versionMatch = [regex]::Match([System.IO.File]::ReadAllText($projectFile), '<Version>([^<]+)</Version>')
        if (-not $versionMatch.Success) { continue }
        if ($versionMatch.Groups[1].Value -eq '0.0.0-not-published-to-nuget') { continue }
        [PSCustomObject]@{ PackageId = $project.Name; Version = $versionMatch.Groups[1].Value; ProjectFolder = "src/$($project.Name)" }
    }
}

function SelectShippedFiles($Package, [string[]]$Files) {
    # The files, paths from the repository root, that the package ships.
    return @($Files | Where-Object { $_ -ne "$($Package.ProjectFolder)/CHANGELOG.md" -and $_ -notmatch '(^|/)(_docs|dev_docs)/' })
}

function GetShippedFilesChangedSince($Package, [string]$Revision) {
    # The package's shipped files that differ between $Revision and HEAD.
    return SelectShippedFiles $Package @(git -C $RepositoryRoot -c core.quotePath=false diff --name-only $Revision HEAD -- $Package.ProjectFolder)
}

function GetUnreleasedChangesStart([string]$ReleaseTag, $Acknowledgement) {
    # The revision a package's unreleased changes count from: its release tag, or the commit its changes are acknowledged
    # up to when that commit comes after the tag. When the tag comes after it, the package was released since, and the
    # acknowledgement no longer counts.
    if (-not $Acknowledgement) { return $ReleaseTag }
    git -C $RepositoryRoot merge-base --is-ancestor $Acknowledgement.Commit HEAD 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "$AcknowledgedUnreleasedChangesFile acknowledges the changes to $($Acknowledgement.PackageId) up to the commit $($Acknowledgement.Commit), which isn't in HEAD's history. Acknowledge them again: C-SimulateRelease -Acknowledge $($Acknowledgement.PackageId) -Reason '<why>'."
    }
    git -C $RepositoryRoot merge-base --is-ancestor $ReleaseTag $Acknowledgement.Commit
    if ($LASTEXITCODE -eq 0) { return $Acknowledgement.Commit }
    return $ReleaseTag
}

function GetUnacknowledgedUnreleasedChanges([string[]]$PackagesBeingPrepared) {
    # The packages that the release would leave behind with unacknowledged changes: each package whose version has a
    # release tag, so that the release doesn't release it, and whose shipped files changed since the later of that tag and
    # the commit its changes are acknowledged up to. $PackagesBeingPrepared are released all the same: C-Prepare-Release
    # is about to bump their versions. Each with its PackageId, Version, ReleaseTag, ChangedSince (that tag or commit)
    # and ChangedFiles.
    $releaseTags = @(git -C $RepositoryRoot tag --list '*/v*')
    $acknowledgements = ReadUnreleasedChangeAcknowledgements
    foreach ($package in GetPublishablePackagesUnderSrc) {
        if ($PackagesBeingPrepared -contains $package.PackageId) { continue }
        $releaseTag = "$($package.PackageId)/v$($package.Version)"
        if ($releaseTags -notcontains $releaseTag) { continue }
        $changedSince = GetUnreleasedChangesStart $releaseTag $acknowledgements[$package.PackageId]
        $changedFiles = @(GetShippedFilesChangedSince $package $changedSince)
        if ($changedFiles.Count -eq 0) { continue }
        [PSCustomObject]@{
            PackageId    = $package.PackageId
            Version      = $package.Version
            ReleaseTag   = $releaseTag
            ChangedSince = $changedSince
            ChangedFiles = $changedFiles
        }
    }
}

function FormatUnreleasedChangesRefusal($UnreleasedChanges) {
    # Why the release is refused, naming each package and its changed files, and the two ways out.
    $lines = @("These packages' shipped files changed since their release, and the release would leave them at their released version:")
    foreach ($change in $UnreleasedChanges) {
        $since = if ($change.ChangedSince -eq $change.ReleaseTag) { $change.ReleaseTag } else { "$($change.ChangedSince.Substring(0, 12)), up to which its changes are acknowledged" }
        $lines += "  $($change.PackageId) $($change.Version), changed since $($since):"
        $lines += @($change.ChangedFiles | ForEach-Object { "    $_" })
    }
    $lines += "For each package, either:"
    $lines += "  - release it: add an entry describing the change to its changelog, src/<PackageId>/CHANGELOG.md, and run C-Prepare-Release, which bumps its version;"
    $lines += "  - or, when the change ships nothing a consumer sees, such as comments only, acknowledge it: C-Prepare-Release or C-SimulateRelease -Acknowledge <PackageId> -Reason '<why>'."
    $lines += "    That records it in $AcknowledgedUnreleasedChangesFile, to commit with the release."
    return $lines -join "`n"
}

# ── The acknowledgements file ──

function ReadUnreleasedChangeAcknowledgements {
    # The acknowledgements in the file, by PackageId: each with its PackageId, Commit and Reason.
    $acknowledgements = @{}
    $lines = (ReadAcknowledgedUnreleasedChangesFile).Lines
    for ($index = 0; $index -lt $lines.Count; $index++) {
        if (IsAcknowledgementsFileCommentOrBlankLine $lines[$index]) { continue }
        $match = [regex]::Match($lines[$index], '^(\S+)\s+([0-9a-f]{40})\s+(\S.*)$')
        if (-not $match.Success) { throw "$AcknowledgedUnreleasedChangesFile, line $($index + 1), isn't '<PackageId> <full commit id> <reason>': $($lines[$index])" }
        $acknowledgements[$match.Groups[1].Value] = [PSCustomObject]@{ PackageId = $match.Groups[1].Value; Commit = $match.Groups[2].Value; Reason = $match.Groups[3].Value.Trim() }
    }
    return $acknowledgements
}

function AcknowledgeUnreleasedChanges([string[]]$PackageId, [string]$Reason) {
    # Records that the changes to these packages' shipped files, up to HEAD, ship nothing a consumer sees, for $Reason.
    if (-not $PackageId -or -not $Reason -or $Reason -match '[\r\n]') {
        throw "-Acknowledge <PackageId> and -Reason '<why>' go together, the reason being one line that says why the changes ship nothing a consumer sees."
    }
    $publishablePackageIds = @(GetPublishablePackagesUnderSrc | ForEach-Object { $_.PackageId })
    foreach ($id in $PackageId) {
        if ($publishablePackageIds -notcontains $id) { throw "-Acknowledge: $id isn't a package under src that is published to nuget.org." }
    }
    $head = git -C $RepositoryRoot rev-parse HEAD
    $acknowledgements = ReadUnreleasedChangeAcknowledgements
    foreach ($id in $PackageId) { $acknowledgements[$id] = [PSCustomObject]@{ PackageId = $id; Commit = $head; Reason = $Reason.Trim() } }
    WriteUnreleasedChangeAcknowledgements $acknowledgements
}

function RemoveUnreleasedChangeAcknowledgements([string[]]$PackageId) {
    # Removes the acknowledgements of packages whose release is being prepared: the release ships their changes.
    $acknowledgements = ReadUnreleasedChangeAcknowledgements
    $toRemove = @($PackageId | Where-Object { $acknowledgements.ContainsKey($_) })
    if ($toRemove.Count -eq 0) { return }
    foreach ($id in $toRemove) { $acknowledgements.Remove($id) }
    WriteUnreleasedChangeAcknowledgements $acknowledgements
}

function WriteUnreleasedChangeAcknowledgements($Acknowledgements) {
    # Writes the acknowledgements, sorted by PackageId, below the file's header, the lines above its first acknowledgement,
    # and says that the file must be committed with the release when it changed.
    $file = ReadAcknowledgedUnreleasedChangesFile
    $headerLineCount = 0
    while ($headerLineCount -lt $file.Lines.Count -and (IsAcknowledgementsFileCommentOrBlankLine $file.Lines[$headerLineCount])) { $headerLineCount++ }
    while ($headerLineCount -gt 0 -and -not $file.Lines[$headerLineCount - 1].Trim()) { $headerLineCount-- }
    $header = @($file.Lines | Select-Object -First $headerLineCount)
    $entries = @($Acknowledgements.Values | Sort-Object PackageId | ForEach-Object { "$($_.PackageId) $($_.Commit) $($_.Reason)" })
    $lines = $header + @(if ($entries.Count -gt 0) { '' }) + $entries
    $text = ($lines -join $file.LineEnding) + $file.LineEnding
    if ($text -eq $file.Text) { return }
    [System.IO.File]::WriteAllText($file.Path, $text, [System.Text.UTF8Encoding]::new($false))
    Write-Host "$AcknowledgedUnreleasedChangesFile changed: commit it with the release." -ForegroundColor Yellow
}

function ReadAcknowledgedUnreleasedChangesFile {
    # The file's text, its lines and its line ending, so that it is written back the way it was.
    $path = Join-Path $RepositoryRoot $AcknowledgedUnreleasedChangesFile
    $text = [System.IO.File]::ReadAllText($path, [System.Text.UTF8Encoding]::new($false))
    $lineEnding = if ($text.Contains("`r`n")) { "`r`n" } else { "`n" }
    return [PSCustomObject]@{ Path = $path; Text = $text; Lines = @($text.TrimEnd("`r", "`n") -split "`r?`n"); LineEnding = $lineEnding }
}

function IsAcknowledgementsFileCommentOrBlankLine([string]$Line) {
    return -not $Line.Trim() -or $Line.TrimStart().StartsWith('#')
}
