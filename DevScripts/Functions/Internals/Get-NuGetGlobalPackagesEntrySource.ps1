# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-NuGetGlobalPackagesEntrySource {
    <#
    .SYNOPSIS
    Gets the package source one package version in the NuGet global packages folder was extracted from, or $null when the folder lacks that version

    .DESCRIPTION
    Reads the entry's .nupkg.metadata, whose source field NuGet writes at extraction: nuget.org's service index for a
    download from nuget.org, a folder path for a local feed. The global packages folder serves the first copy of a
    version any restore on the machine extracted, whatever feed later serves that version, so this is the one way
    to tell which build of a version the machine's builds use.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesDirectory,
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $metadataPath = Join-Path $GlobalPackagesDirectory "$($PackageId.ToLowerInvariant())/$($Version.ToLowerInvariant())/.nupkg.metadata"
    if (-not (Test-Path -LiteralPath $metadataPath)) { return $null }
    return (Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json).source
}
