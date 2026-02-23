# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function Ensure-IsrPackage {
    <#
    .SYNOPSIS
    Ensures the ISR (InternalizedSourceReferences) build tool package exists in nupkgs/

    .DESCRIPTION
    ThreadingCE needs ISR as a PackageReference (it's a build-time MSBuild task, not a library).
    ISR has zero Compze dependencies so it can always be packed independently.
    This function packs ISR if no ISR package exists in nupkgs/.
    Uses a dev timestamp suffix to avoid NuGet cache staleness.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $nupkgsPath = Join-Path $script:CompzeRoot "nupkgs"

    if (-not (Test-Path $nupkgsPath)) {
        New-Item -ItemType Directory -Path $nupkgsPath | Out-Null
    }

    # Check if any ISR package already exists
    $existing = Get-ChildItem $nupkgsPath -Filter "Compze.Build.InternalizedSourceReferences.*.nupkg" -ErrorAction SilentlyContinue
    if ($existing) {
        return
    }

    # Pack ISR with a dev timestamp
    $timestamp = Get-Date -Format "yyyyMMddHHmmss"
    $suffix = "dev.$timestamp"
    $isrProjPath = Join-Path $script:CompzeRoot "src" "Compze.Build.InternalizedSourceReferences" "src" "Compze.Build.InternalizedSourceReferences" "Compze.Build.InternalizedSourceReferences.csproj"

    dotnet pack $isrProjPath --configuration Release --output $nupkgsPath /p:CompzeLocalPackSuffix=$suffix
    if ($LASTEXITCODE -ne 0) {
        Write-Error "Ensure-IsrPackage: Failed to pack ISR!"
        return
    }
}
