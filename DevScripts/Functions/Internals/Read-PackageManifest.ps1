# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-PackageManifest {
    <#
    .SYNOPSIS
    Reads a package file's id, version and the commit it was built from (see ConvertFrom-PackageManifestXml)

    .DESCRIPTION
    For the package files this machine packs, which all record their commit: one that does not is refused, because
    whether a published build is still the code that ships rests on knowing which code it is.

    .OUTPUTS
    Object with PackageId, Version and RepositoryCommit
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$NupkgPath
    )

    $package = [System.IO.Compression.ZipFile]::OpenRead($NupkgPath)
    try {
        # The manifest is the one .nuspec at the package root.
        $manifestEntry = $package.Entries | Where-Object { $_.FullName -notmatch '/' -and $_.Name -like '*.nuspec' } | Select-Object -First 1
        if (-not $manifestEntry) { throw "$NupkgPath contains no .nuspec at its root, so it is not a NuGet package." }
        $reader = [System.IO.StreamReader]::new($manifestEntry.Open())
        try { $manifestXml = $reader.ReadToEnd() } finally { $reader.Dispose() }
    } finally {
        $package.Dispose()
    }
    $packageManifest = ConvertFrom-PackageManifestXml -ManifestXml $manifestXml
    if (-not $packageManifest.RepositoryCommit) {
        throw "$NupkgPath records no repository commit, so nothing can tell which code it was built from."
    }
    return $packageManifest
}
