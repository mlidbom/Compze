# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-CompzeLocalBuildsInGlobalPackages {
    <#
    .SYNOPSIS
    Gets the Compze package versions in the NuGet global packages folder that were extracted from a local folder feed rather than from nuget.org

    .DESCRIPTION
    Reads each Compze entry's source (Get-NuGetGlobalPackagesEntrySource). Leaves out the timestamped dev versions
    C-Pack makes (dev.YYYYMMDDHHMMSS), which nuget.org never has, and entries whose metadata predates NuGet recording
    a source, since nothing tells where those came from.

    .OUTPUTS
    Objects with PackageId (lowercase, as the folder is named), Version and Source
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesDirectory
    )

    Get-ChildItem -LiteralPath $GlobalPackagesDirectory -Directory -Filter "compze.*" | ForEach-Object {
        $packageId = $_.Name
        Get-ChildItem -LiteralPath $_.FullName -Directory | Where-Object { $_.Name -notmatch 'dev\.\d{14}$' } | ForEach-Object {
            $source = Get-NuGetGlobalPackagesEntrySource -GlobalPackagesDirectory $GlobalPackagesDirectory -PackageId $packageId -Version $_.Name
            if ($source -and $source -notlike "https://api.nuget.org/*") {
                [PSCustomObject]@{ PackageId = $packageId; Version = $_.Name; Source = $source }
            }
        }
    }
}
