# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Switch-NuGetGlobalPackagesEntry {
    <#
    .SYNOPSIS
    Puts the version folder By in the place of one package version in the NuGet global packages folder, two renames apart; returns $false, changing nothing, when the entry there is in use

    .DESCRIPTION
    The entry there is first renamed aside, which Windows refuses while any file in it is open or memory-mapped. By, on
    the same volume, is then renamed into its place, and the old entry deleted.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$GlobalPackagesDirectory,
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version,
        [Parameter(Mandatory = $true)]
        [string]$By
    )

    $accessDenied = -2147024891 # 0x80070005: what renaming a folder holding an open or memory-mapped file fails with (observed 2026-11-10)
    $entry = Join-Path $GlobalPackagesDirectory "$($PackageId.ToLowerInvariant())/$($Version.ToLowerInvariant())"
    $aside = "$entry.being-replaced-$([guid]::NewGuid().ToString('N'))"
    try {
        Rename-Item -LiteralPath $entry -NewName (Split-Path $aside -Leaf) -ErrorAction Stop
    } catch [System.IO.IOException] {
        if ($_.Exception.HResult -ne $accessDenied) { throw }
        return $false
    }
    try {
        [System.IO.Directory]::Move($By, $entry)
    } catch {
        [System.IO.Directory]::Move($aside, $entry)
        throw
    }
    Remove-Item -LiteralPath $aside -Recurse -Force
    return $true
}
