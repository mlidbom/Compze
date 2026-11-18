# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Publish-ToPendingReleaseFeed {
    <#
    .SYNOPSIS
    Publishes the package versions the next release will publish to this machine's pending-release feed

    .DESCRIPTION
    The pending-release feed is a local NuGet folder feed that every repository on this machine restores from
    (see Get-PendingReleaseFeedDirectory). It holds only versions published here ahead of nuget.org, so a consumer
    repository can use a new Compze version the moment it is published here, instead of waiting for CI, the release
    workflow and nuget.org's validation: half an hour to over an hour in all.

    Packs with the real versions (C-Pack -CI) and publishes exactly what the release would publish: the versions without
    a release tag, each with its changelog entry. The selection is the release workflow's own
    (.github/scripts/Get-PackagesToRelease.ps1).

    A version published here must be the version that ships, so:
    - It packs what HEAD holds, in a temporary clean worktree of HEAD that it removes afterwards
      (Invoke-InCleanWorktreeOfHead), whatever the checkout's working tree holds. Uncommitted changes in the checkout
      don't stop it, and are not published: it warns that they exist. It builds the solution from scratch there.
    - A version nuget.org already has is refused.
    - So is a set of versions the release would refuse because the post-release binding check fails for it
      (.github/scripts/Invoke-PostReleaseBindingCheck.ps1): a consumer here would get the same runtime failures.
    - A version already in the feed is republished only when its package's source (src/<PackageId>) changed since it was
      published, and only with -Replace. Without it, bump the version instead.

    The output names each version published; reference exactly those versions from the consumer. A consumer commit
    referencing one fails CI until nuget.org has it, so push it afterwards. Once nuget.org has a version,
    C-Clear-ReleasedLocalBuilds removes the local build of it, and a user-level SessionStart hook runs that in
    every Claude session on this machine.

    .PARAMETER Replace
    Replace a version already in the feed whose package source has changed since: the feed gets the new build, and the
    old one is removed from the NuGet global packages folder so every restore on this machine extracts the new one.

    .EXAMPLE
    C-Publish-ToPendingReleaseFeed
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$Replace
    )

    # Stop at the first error, inherited by every function this calls: a failed lookup must never read as
    # "nuget.org has not released it" or "has released it", and carry on deleting or publishing on that misreading.
    $ErrorActionPreference = 'Stop'

    $uncommittedChanges = @(git -C $script:CompzeRoot status --porcelain)
    if ($uncommittedChanges.Count -gt 0) {
        Write-Warning "C-Publish-ToPendingReleaseFeed: the checkout has $($uncommittedChanges.Count) uncommitted changes, which are not published: it publishes what HEAD holds."
    }

    # The selection reads local tags, so refresh them first: a clone whose tags lag the remote sees released versions as unreleased.
    $fetchResult = git -C $script:CompzeRoot fetch --all --tags --quiet 2>&1
    if ($LASTEXITCODE -ne 0) {
        Write-Warning "Could not fetch the latest tags, so the selection may be based on stale tags.`n$fetchResult"
    }

    Invoke-InCleanWorktreeOfHead {
        C-Pack -CI

        $nupkgsDirectory = Join-Path $script:CompzeRoot "nupkgs"
        Push-Location $script:CompzeRoot
        try {
            $packagesToRelease = @(& (Join-Path $script:CompzeRoot ".github/scripts/Get-PackagesToRelease.ps1") -NupkgsPath $nupkgsDirectory)
            $selectionExitCode = $LASTEXITCODE
        } finally {
            Pop-Location
        }
        if ($selectionExitCode -ne 0) {
            throw "C-Publish-ToPendingReleaseFeed: the release would refuse these packages (see above), so nothing was published."
        }
        if ($packagesToRelease.Count -eq 0) {
            Write-Host "Nothing to publish: every packed version already has a release tag."
            return
        }

        $restorableOnNuGetOrg = Get-NuGetOrgRestorableVersions -PackageId @($packagesToRelease | ForEach-Object { $_.PackageName })
        $alreadyOnNuGetOrg = @($packagesToRelease | Where-Object { $restorableOnNuGetOrg[$_.PackageName] -contains $_.Version.ToLowerInvariant() })
        if ($alreadyOnNuGetOrg.Count -gt 0) {
            throw ("C-Publish-ToPendingReleaseFeed: nuget.org already has $(($alreadyOnNuGetOrg | ForEach-Object { "$($_.PackageName) $($_.Version)" }) -join ', '), " +
                   "but no release tag exists for it: a release likely crashed after pushing (C-SimulateRelease's summary shows it as TAG MISSING). Nothing was published.")
        }

        Push-Location $script:CompzeRoot
        try {
            & (Join-Path $script:CompzeRoot ".github/scripts/Invoke-PostReleaseBindingCheck.ps1") -NupkgsPath $nupkgsDirectory
            $bindingCheckExitCode = $LASTEXITCODE
        } finally {
            Pop-Location
        }
        if ($bindingCheckExitCode -ne 0) {
            throw "C-Publish-ToPendingReleaseFeed: the release would refuse these versions, since Compze packages would fail at runtime against each other (see above). Nothing was published."
        }

        # Decide for every package before publishing any, so a refusal publishes nothing.
        $feedDirectory = Initialize-PendingReleaseFeed
        $headCommit = git -C $script:CompzeRoot rev-parse HEAD
        $publications = @(foreach ($package in $packagesToRelease) {
            $nupkgFileName = "$($package.PackageName).$($package.Version).nupkg"
            $publishedNupkg = Join-Path $feedDirectory $nupkgFileName
            $publishedFrom = if (Test-Path -LiteralPath $publishedNupkg) { (Read-PackageManifest -NupkgPath $publishedNupkg).RepositoryCommit } else { $null }
            $isNew = -not $publishedFrom
            $isChanged = (-not $isNew) -and (Test-PackageSourceChangedBetweenCommits -PackageId $package.PackageName -FromCommit $publishedFrom -ToCommit $headCommit)
            [PSCustomObject]@{
                Package        = $package
                PackedNupkg    = Join-Path $nupkgsDirectory $nupkgFileName
                PublishedNupkg = $publishedNupkg
                PublishedFrom  = $publishedFrom
                IsNew          = $isNew
                IsChanged      = $isChanged
            }
        })

        $refused = @($publications | Where-Object { $_.IsChanged -and -not $Replace })
        if ($refused.Count -gt 0) {
            throw ("C-Publish-ToPendingReleaseFeed: a version published here must be the code that ships, and " +
                   "$(($refused | ForEach-Object { "src/$($_.Package.PackageName) changed since commit $($_.PublishedFrom.Substring(0, 9)) published $($_.Package.PackageName) $($_.Package.Version) here" }) -join '; '). " +
                   "Bump the version, or rerun with -Replace to replace the published build on this machine. Nothing was published.")
        }

        $replacements = @($publications | Where-Object { $_.IsChanged })
        if ($replacements.Count -gt 0) {
            $globalPackagesDirectory = Get-NuGetGlobalPackagesDirectory
            foreach ($replacement in $replacements) {
                if (-not (Remove-NuGetGlobalPackagesEntry -GlobalPackagesDirectory $globalPackagesDirectory -PackageId $replacement.Package.PackageName -Version $replacement.Package.Version)) {
                    throw ("C-Publish-ToPendingReleaseFeed: not replacing $($replacement.Package.PackageName) $($replacement.Package.Version): its old build in the NuGet global packages folder is in use. " +
                           "Run 'dotnet build-server shutdown' (and restart the IDE or NCrunch if they hold it), then rerun. Nothing was published.")
                }
            }
        }

        $toPublish = @($publications | Where-Object { $_.IsNew -or $_.IsChanged })
        if ($toPublish.Count -eq 0) {
            Write-Host "Nothing to publish: the pending-release feed already has every version the release would publish, built from the same package source."
            return
        }
        foreach ($publication in $toPublish) {
            Copy-Item -LiteralPath $publication.PackedNupkg -Destination $publication.PublishedNupkg -Force
            Write-Host "Published $($publication.Package.PackageName) $($publication.Package.Version) to the pending-release feed."
        }
    }

    $global:LASTEXITCODE = 0
}
