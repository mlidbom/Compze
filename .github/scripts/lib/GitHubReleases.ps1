function CreateGitHubRelease($Package, [string]$NupkgsPath, [switch]$DryRun, [switch]$Verbose) {
    $releaseName = "$($Package.PackageName) v$($Package.Version)"

    $assets = @("nupkg", "snupkg") |
        ForEach-Object { Join-Path $NupkgsPath "$($Package.PackageName).$($Package.Version).$_" } |
        Where-Object { Test-Path $_ }

    $prereleaseFlag = @()
    if ($Package.Version -match '-') {
        $prereleaseFlag = @("--prerelease")
    }

    if ($Verbose) {
        $assetNames = $assets | ForEach-Object { Split-Path $_ -Leaf }
        Write-Host "  Create GitHub release (prerelease: $($prereleaseFlag.Count -gt 0))"
        Write-Host "    Assets: $($assetNames -join ', ')"
        Write-Host "    Changelog:"
    }
    Write-Host "    ┌──────────────────────────────────"
    $Package.ChangelogContent -split "`n" | ForEach-Object { Write-Host "    │ $_" }
    Write-Host "    └──────────────────────────────────"
    if (-not $DryRun) {
        $tempNotesFile = [System.IO.Path]::GetTempFileName()
        try {
            [IO.File]::WriteAllText($tempNotesFile, $Package.ChangelogContent)

            gh release create $Package.Tag `
                --title $releaseName `
                --notes-file $tempNotesFile `
                @prereleaseFlag `
                @assets

            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to create release for $releaseName"
                exit 1
            }
        } finally {
            Remove-Item $tempNotesFile -ErrorAction SilentlyContinue
        }
    }
}

