function FormatTag([string]$PackageName, [string]$Version) {
    return "$PackageName/v$Version"
}

function TestTagExists([string]$Tag) {
    git rev-parse "refs/tags/$Tag" 2>$null | Out-Null
    return $LASTEXITCODE -eq 0
}

function PackagesWithNoMatchingReleaseTag($Packages) {
    return @($Packages | Where-Object { -not (TestTagExists $_.Tag) })
}

function CreateTag($Package, [switch]$DryRun, [switch]$Verbose) {
    if ($Verbose) { Write-Host "  Create tag $($Package.Tag)" }
    if (-not $DryRun) {
        git tag $Package.Tag -m "$($Package.PackageName) v$($Package.Version)"
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to create tag $($Package.Tag)"
            exit 1
        }
    }
}

function PushTag($Package, [switch]$DryRun, [switch]$Verbose) {
    if ($Verbose) { Write-Host "  Push tag $($Package.Tag)" }
    if (-not $DryRun) {
        git push origin $Package.Tag
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Failed to push tag $($Package.Tag)"
            exit 1
        }
    }
}
