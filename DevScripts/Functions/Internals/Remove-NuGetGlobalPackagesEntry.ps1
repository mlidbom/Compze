# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Remove-NuGetGlobalPackagesEntry {
    <#
    .SYNOPSIS
    Removes one package version from the NuGet global packages folder, all or nothing; returns whether it is gone

    .DESCRIPTION
    The next restore needing the version extracts it afresh, from whichever source has it.

    The version's folder is first renamed out of NuGet's way in one step. Windows refuses that while any file in it is
    open or memory-mapped, as a build server, the IDE or NCrunch may hold one: then nothing changes and this returns
    $false. A plain recursive delete would instead remove what it could and leave a half-deleted entry that NuGet still
    treats as extracted. `dotnet build-server shutdown` releases the build servers' hold; the IDE and NCrunch may need
    a restart.
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

    $accessDenied = -2147024891 # 0x80070005: what renaming a folder holding an open or memory-mapped file fails with (observed 2026-11-10)
    $entry = Join-Path $GlobalPackagesDirectory "$($PackageId.ToLowerInvariant())/$($Version.ToLowerInvariant())"
    if (-not (Test-Path -LiteralPath $entry)) { return $true }

    $removalName = "$(Split-Path $entry -Leaf).being-removed-$([guid]::NewGuid().ToString('N'))"
    try {
        Rename-Item -LiteralPath $entry -NewName $removalName -ErrorAction Stop
    } catch [System.IO.IOException] {
        if ($_.Exception.HResult -ne $accessDenied) { throw }
        return $false
    }
    Remove-Item -LiteralPath (Join-Path (Split-Path $entry -Parent) $removalName) -Recurse -Force
    return $true
}
