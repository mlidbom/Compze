function CreateGitHubRelease {
    [CmdletBinding(SupportsShouldProcess)]
    param($Package, [string]$NupkgsPath)

    $releaseName = "$($Package.PackageName) v$($Package.Version)"

    $tempNotesFile = [System.IO.Path]::GetTempFileName()
    try {
        [IO.File]::WriteAllText($tempNotesFile, $Package.ChangelogContent)

        $assets = @("nupkg", "snupkg") |
            ForEach-Object { Join-Path $NupkgsPath "$($Package.PackageName).$($Package.Version).$_" } |
            Where-Object { Test-Path $_ }

        $prereleaseFlag = @()
        if ($Package.Version -match '-') {
            $prereleaseFlag = @("--prerelease")
        }

        $assetNames = $assets | ForEach-Object { Split-Path $_ -Leaf }
        Write-Host "  Will Create GitHub release: $releaseName (prerelease: $($prereleaseFlag.Count -gt 0))"
        Write-Host "    Assets: $($assetNames -join ', ')"
        Write-Host "    Changelog:"
        $Package.ChangelogContent -split "`n" | ForEach-Object { Write-Host "      $_" }
        if ($PSCmdlet.ShouldProcess($releaseName, "Create GitHub release")) {
            gh release create $Package.Tag `
                --title $releaseName `
                --notes-file $tempNotesFile `
                @prereleaseFlag `
                @assets

            if ($LASTEXITCODE -ne 0) {
                Write-Error "Failed to create release for $releaseName"
                exit 1
            }
        }
    } finally {
        Remove-Item $tempNotesFile -ErrorAction SilentlyContinue
    }
}

