function PushToNuGet {
    [CmdletBinding(SupportsShouldProcess)]
    param($Package, [string]$NupkgsPath)

    $nupkgPath = Join-Path $NupkgsPath "$($Package.PackageName).$($Package.Version).nupkg"
    if (-not (Test-Path $nupkgPath)) {
        Write-Error "Package file not found: $nupkgPath"
        exit 1
    }

    Write-Host "  Will push $($Package.PackageName).$($Package.Version).nupkg to NuGet.org"
    if ($PSCmdlet.ShouldProcess($nupkgPath, "Push to NuGet.org")) {
        dotnet nuget push $nupkgPath `
            --api-key $env:NUGET_API_KEY `
            --source "https://api.nuget.org/v3/index.json" `
            --skip-duplicate

        if ($LASTEXITCODE -ne 0) {
            Write-Error "NuGet push failed for $($Package.PackageName) v$($Package.Version)"
            exit 1
        }
    }
}
