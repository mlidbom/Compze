# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-NuGetGlobalPackagesDirectory {
    <#
    .SYNOPSIS
    Gets the NuGet global packages folder: the machine-wide cache every restore extracts packages into and every build reads them from

    .DESCRIPTION
    Asks the dotnet CLI rather than assuming ~/.nuget/packages, so a NUGET_PACKAGES override or a NuGet.Config
    globalPackagesFolder setting is honored.
    #>
    [CmdletBinding()]
    param()

    $listing = & dotnet nuget locals global-packages --list 2>$null
    $match = $listing | Select-String 'global-packages:\s*(.+)' | Select-Object -First 1
    if (-not $match) {
        throw "Could not determine the NuGet global packages folder from 'dotnet nuget locals global-packages --list': $listing"
    }
    return $match.Matches[0].Groups[1].Value.Trim()
}
