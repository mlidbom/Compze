# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function ConvertFrom-PackageManifestXml {
    <#
    .SYNOPSIS
    Reads a package's id, version and the commit it was built from, out of the text of its manifest (its .nuspec)

    .DESCRIPTION
    The commit is the repository element's commit attribute, which the .NET SDK's source control integration stamps
    into every package it packs, here and in the release workflow alike. It is what tells two builds of one version apart.
    Older Compze releases predate the stamp, so a manifest may lack it.

    .OUTPUTS
    Object with PackageId, Version and RepositoryCommit ($null when the manifest records none)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ManifestXml
    )

    [xml]$manifest = $ManifestXml
    $metadata = $manifest.package.metadata
    $repositoryCommit = if ($metadata.repository -and $metadata.repository.commit) { $metadata.repository.commit } else { $null }
    return [PSCustomObject]@{
        PackageId        = $metadata.id
        Version          = $metadata.version
        RepositoryCommit = $repositoryCommit
    }
}
