# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Copy-PackageReferences {
    <#
    .SYNOPSIS
    Copies all PackageReference entries from one .csproj to another

    .DESCRIPTION
    Reads all PackageReference elements from the source .csproj file and adds them
    to the target .csproj file, preserving child elements (e.g., PrivateAssets, IncludeAssets).
    Skips any package references that already exist in the target.

    .PARAMETER SourceCsprojPath
    Path to the .csproj file to copy package references from

    .PARAMETER TargetCsprojPath
    Path to the .csproj file to copy package references to
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceCsprojPath,

        [Parameter(Mandatory = $true)]
        [string]$TargetCsprojPath
    )

    if (-not (Test-Path $SourceCsprojPath)) {
        Write-Error "Source project file not found: $SourceCsprojPath"
        return
    }

    if (-not (Test-Path $TargetCsprojPath)) {
        Write-Error "Target project file not found: $TargetCsprojPath"
        return
    }

    [xml]$sourceXml = Get-Content $SourceCsprojPath
    $packageReferences = $sourceXml.SelectNodes("//PackageReference[@Include]")

    if (-not $packageReferences -or $packageReferences.Count -eq 0) {
        return
    }

    [xml]$targetXml = Get-Content $TargetCsprojPath

    # Find or create ItemGroup for PackageReferences
    $itemGroup = $targetXml.SelectSingleNode("//ItemGroup[PackageReference]")
    if (-not $itemGroup) {
        $itemGroup = $targetXml.CreateElement("ItemGroup")
        $targetXml.DocumentElement.AppendChild($itemGroup) | Out-Null
    }

    $changed = $false

    foreach ($pkgRef in $packageReferences) {
        $pkgName = $pkgRef.GetAttribute("Include")

        # Skip if already exists
        $existing = $targetXml.SelectNodes("//PackageReference[@Include]") |
            Where-Object { $_.GetAttribute("Include") -eq $pkgName } |
            Select-Object -First 1
        if ($existing) { continue }

        # Clone the package reference node into the target project's XML
        $importedNode = $targetXml.ImportNode($pkgRef, $true)
        $itemGroup.AppendChild($importedNode) | Out-Null
        $changed = $true
    }

    if ($changed) {
        Save-XmlWithThreeSpacesIndentation -Xml $targetXml -Path $TargetCsprojPath
    }
}
