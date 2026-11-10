# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Read-NuGetOrgPackageManifest {
    <#
    .SYNOPSIS
    Reads the id, version and build commit of the package nuget.org serves for one version (see ConvertFrom-PackageManifestXml)

    .OUTPUTS
    Object with PackageId, Version and RepositoryCommit ($null for an older release that predates the commit stamp)
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$PackageId,
        [Parameter(Mandatory = $true)]
        [string]$Version
    )

    $id = $PackageId.ToLowerInvariant()
    $manifestXml = (Invoke-WebRequest -Uri "https://api.nuget.org/v3-flatcontainer/$id/$($Version.ToLowerInvariant())/$id.nuspec" -TimeoutSec 30).Content
    return ConvertFrom-PackageManifestXml -ManifestXml $manifestXml
}
