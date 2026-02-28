function ParsePackageNameAndVersionFromNupkgFilename([string]$Filename) {
    # NuGet filenames: PackageName.Major.Minor.Patch[-prerelease].nupkg
    # The version starts at the first dot-separated segment that begins with a digit (after index 0).
    $segments = $Filename -split '\.'
    for ($i = 1; $i -lt $segments.Length; $i++) {
        if ($segments[$i] -match '^\d') {
            $packageName = ($segments[0..($i - 1)]) -join '.'
            $version = ($segments[$i..($segments.Length - 1)]) -join '.'
            return [PSCustomObject]@{
                PackageName = $packageName
                Version     = $version
                Tag         = FormatTag $packageName $version
            }
        }
    }
    return $null
}

function GetPackagesFromNupkgFiles([string]$NupkgsPath) {
    $nupkgFiles = Get-ChildItem -Path $NupkgsPath -Filter "*.nupkg" |
        Where-Object { $_.Name -notlike "*.symbols.nupkg" }

    $packages = @()
    foreach ($nupkg in $nupkgFiles) {
        $parsed = ParsePackageNameAndVersionFromNupkgFilename $nupkg.BaseName
        if (-not $parsed) {
            Write-Error "Could not parse package name and version from filename: $($nupkg.BaseName)"
            exit 1
        }
        $packages += $parsed
    }
    return $packages
}
