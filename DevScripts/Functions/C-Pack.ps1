# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Pack {
    <#
    .SYNOPSIS
    Packs all Compze NuGet packages

    .DESCRIPTION
    Builds in Release configuration and packs all packable projects into the nupkgs/ folder at the repository root.
    Includes both the main solution and Compze.InternalizedSourceReferences.

    .PARAMETER NoBuild
    Skip building before packing. Use when you've already built in Release configuration.

    .EXAMPLE
    C-Pack
    Builds in Release and packs all projects

    .EXAMPLE
    C-Pack -NoBuild
    Packs without rebuilding (assumes Release build already done)
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$NoBuild
    )

    $nupkgsPath = Join-Path $script:CompzeRoot "nupkgs"
    $isrSlnPath = Join-Path $script:CompzeRoot "Compze.InternalizedSourceReferences" "Compze.InternalizedSourceReferences.slnx"
    $isrCsprojPath = Join-Path $script:CompzeRoot "Compze.InternalizedSourceReferences" "src" "Compze.InternalizedSourceReferences" "Compze.InternalizedSourceReferences.csproj"

    # Clean output folder
    if (Test-Path $nupkgsPath) {
        Remove-Item "$nupkgsPath\*" -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $nupkgsPath | Out-Null
    }

    $noBuildArg = if ($NoBuild) { "--no-build" } else { $null }

    # Pack Compze.InternalizedSourceReferences first (other projects depend on it)
    if (-not $NoBuild) {
        dotnet build $isrSlnPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Compze.InternalizedSourceReferences build failed!"
            return
        }
    }

    $packArgs = @("pack", $isrCsprojPath, "--configuration", "Release", "--output", $nupkgsPath)
    if ($NoBuild) { $packArgs += "--no-build" }
    dotnet @packArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Compze.InternalizedSourceReferences pack failed!"
        return
    }

    # Build and pack main solution
    if (-not $NoBuild) {
        # Ensure pluggable component configuration files exist before building
        C-Set-PluggableComponents -EnsureValid

        dotnet build $script:CompzeSolutionPath --configuration Release
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Solution build failed!"
            return
        }
    }

    $packArgs = @("pack", $script:CompzeSolutionPath, "--configuration", "Release", "--output", $nupkgsPath)
    if ($NoBuild) { $packArgs += "--no-build" }
    dotnet @packArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Solution pack failed!"
        return
    }

    # Summary
    $packages = Get-ChildItem $nupkgsPath -Filter "*.nupkg"
    Write-Host "$($packages.Count) packages created in nupkgs/"

    $global:LASTEXITCODE = 0
}
