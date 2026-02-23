# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Pack {
    <#
    .SYNOPSIS
    Packs all Compze NuGet packages for local development

    .DESCRIPTION
    Packs all packable projects with unique timestamped versions for local development.
    This avoids NuGet cache staleness — each pack gets a fresh cache entry, eliminating
    file-lock issues when overwriting cached DLLs.

    CI/publish builds use the real version from each .csproj. Local dev packs append
    a '.dev.YYYYMMDDHHMMSS' suffix (e.g., 0.1.0-alpha.3.dev.20260223143052).

    The ISR (InternalizedSourceReferences) build tool is packed first since ThreadingCE
    needs it as a PackageReference.

    .EXAMPLE
    C-Pack
    Packs all projects with timestamped dev versions, clears NuGet cache for Compze packages.

    .EXAMPLE
    C-Pack -CI
    Packs using the real version from each .csproj (for publishing).
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$CI
    )

    $nupkgsPath = Join-Path $script:CompzeRoot "nupkgs"

    if (-not (Test-Path $nupkgsPath)) {
        New-Item -ItemType Directory -Path $nupkgsPath | Out-Null
    }

    # Build version suffix for local dev packs
    $versionArgs = @()
    if (-not $CI) {
        $timestamp = Get-Date -Format "yyyyMMddHHmmss"
        $suffix = "dev.$timestamp"
        $versionArgs = @("/p:CompzeLocalPackSuffix=$suffix")
        # Remove old dev packages to keep the feed clean
        Get-ChildItem $nupkgsPath -Filter "*.dev.*.nupkg" -ErrorAction SilentlyContinue | Remove-Item -Force
        Get-ChildItem $nupkgsPath -Filter "*.dev.*.snupkg" -ErrorAction SilentlyContinue | Remove-Item -Force
    }

    # Ensure pluggable component configuration files exist before building
    C-Set-PluggableComponents -EnsureValid

    # Pack ISR first — ThreadingCE needs it as a PackageReference from the local feed
    $isrProjPath = Join-Path $script:CompzeRoot "src" "Compze.Build.InternalizedSourceReferences" "src" "Compze.Build.InternalizedSourceReferences" "Compze.Build.InternalizedSourceReferences.csproj"
    dotnet pack $isrProjPath --configuration Release --output $nupkgsPath @versionArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "C-Pack: ISR pack failed!"
        return
    }

    # Pack main solution
    dotnet pack $script:CompzeSolutionPath --configuration Release --output $nupkgsPath @versionArgs
    if ($LASTEXITCODE -ne 0) {
        Write-Error "C-Pack: Solution pack failed!"
        return
    }

    # Summary
    $packages = Get-ChildItem $nupkgsPath -Filter "*.nupkg"
    Write-Host "$($packages.Count) packages created in nupkgs/"
    if (-not $CI) {
        Write-Host "Local dev version suffix: $suffix"
    }

    $global:LASTEXITCODE = 0
}
