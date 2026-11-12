# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Write-ChangelogSectionsIntoPackages {
    <#
    .SYNOPSIS
    Writes each package file's changelog section for its version into the package, as its release notes

    .DESCRIPTION
    Release notes are what nuget.org's package page and NuGet clients show as what a version changed. A package whose
    changelog has no section for its version gets none: the release refuses to publish such a package anyway
    (.github/scripts/lib/ReleaseDetails.ps1).

    The changelog sections are read as the release scripts read them (.github/scripts/lib/Changelogs.ps1), so a
    package's release notes are the text of its GitHub release.

    .PARAMETER NupkgsDirectory
    The directory of the package files, as C-Pack -CI leaves them.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$NupkgsDirectory
    )

    . (Join-Path $script:CompzeRoot ".github/scripts/lib/Changelogs.ps1")
    . (Join-Path $script:CompzeRoot ".github/scripts/lib/PackageManifests.ps1")

    # FindProjectChangelogPath finds changelogs relative to the repository root.
    Push-Location $script:CompzeRoot
    try {
        foreach ($nupkg in @(Get-ChildItem -LiteralPath $NupkgsDirectory -Filter "*.nupkg" | Where-Object Extension -eq '.nupkg')) {
            $manifest = ReadNupkgManifest $nupkg.FullName
            $changelogPath = FindProjectChangelogPath $manifest.PackageId
            if (-not $changelogPath) { continue }
            $changelogSection = ExtractChangelogSectionForVersion $changelogPath $manifest.Version
            if ($changelogSection) { WriteReleaseNotesIntoNupkg $nupkg.FullName $changelogSection }
        }
    } finally {
        Pop-Location
    }
}
