# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Pack {
    <#
    .SYNOPSIS
    Packs all Compze NuGet packages

    .DESCRIPTION
    Builds in Release configuration. All packable projects have GeneratePackageOnBuild enabled,
    so building automatically produces packages in the nupkgs/ folder at the repository root.
    Old package versions are cleaned up before building.

    .EXAMPLE
    C-Pack
    Cleans old packages, builds in Release (which auto-packs all projects)
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param()

    $nupkgsPath = Join-Path $script:CompzeRoot "nupkgs"

    # Clean output folder
    if (Test-Path $nupkgsPath) {
        Remove-Item "$nupkgsPath\*" -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $nupkgsPath | Out-Null
    }

    # Ensure pluggable component configuration files exist before building
    C-Set-PluggableComponents -EnsureValid

    # Build ISR first — ThreadingCE needs it as a PackageReference from the local feed
    $isrSlnPath = Join-Path $script:CompzeRoot "src" "Compze.Build.InternalizedSourceReferences" "Compze.Build.InternalizedSourceReferences.slnx"
    dotnet build $isrSlnPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Compze.Build.InternalizedSourceReferences build failed!"
        return
    }

    # Build main solution — GeneratePackageOnBuild produces all packages automatically
    dotnet build $script:CompzeSolutionPath --configuration Release
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Solution build failed!"
        return
    }

    # Summary
    $packages = Get-ChildItem $nupkgsPath -Filter "*.nupkg"
    Write-Host "$($packages.Count) packages created in nupkgs/"

    $global:LASTEXITCODE = 0
}
