# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Add-InternedSourceReference {
    <#
    .SYNOPSIS
    Configures a project to internalize source from another project directory

    .DESCRIPTION
    Adds a PackageReference to Compze.Build.InternalizedSourceReferences and sets
    InternalizeSourceFrom/InternalizeSourceTo properties in the consumer project.
    This allows the consumer to compile an internal copy of the source project's code,
    which is useful for resolving circular dependency scenarios.

    .PARAMETER ConsumerCsprojPath
    Path to the .csproj file that will internalize the source

    .PARAMETER SourceProjectDir
    Path to the directory of the project whose source will be internalized
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ConsumerCsprojPath,

        [Parameter(Mandatory = $true)]
        [string]$SourceProjectDir
    )

    if (-not (Test-Path $ConsumerCsprojPath)) {
        Write-Error "Project file not found: $ConsumerCsprojPath"
        return
    }

    $consumerDir = Split-Path -Parent $ConsumerCsprojPath

    $sourceRelativePath = [System.IO.Path]::GetRelativePath($consumerDir, $SourceProjectDir)

    # Add PackageReference to Compze.Build.InternalizedSourceReferences
    [xml]$xml = Get-Content $ConsumerCsprojPath

    $existingPkgRef = $xml.SelectNodes("//PackageReference[@Include='Compze.Build.InternalizedSourceReferences']") | Select-Object -First 1
    if (-not $existingPkgRef) {
        # Find or create an ItemGroup for PackageReferences
        $pkgItemGroup = $xml.SelectNodes("//ItemGroup[PackageReference]") | Select-Object -First 1
        if (-not $pkgItemGroup) {
            $pkgItemGroup = $xml.CreateElement("ItemGroup")
            $xml.DocumentElement.AppendChild($pkgItemGroup) | Out-Null
        }
        $pkgRef = $xml.CreateElement("PackageReference")
        $pkgRef.SetAttribute("Include", "Compze.Build.InternalizedSourceReferences")
        $pkgRef.SetAttribute("Version", "*-*")
        $pkgRef.SetAttribute("PrivateAssets", "all")
        $pkgItemGroup.AppendChild($pkgRef) | Out-Null
    }

    # Add PropertyGroup with InternalizeSourceFrom/To
    $propertyGroup = $xml.CreateElement("PropertyGroup")

    $fromProp = $xml.CreateElement("InternalizeSourceFrom")
    $fromProp.InnerText = $sourceRelativePath
    $propertyGroup.AppendChild($fromProp) | Out-Null

    $toProp = $xml.CreateElement("InternalizeSourceTo")
    $toProp.InnerText = '$(MSBuildProjectDirectory)\InternalizedSource'
    $propertyGroup.AppendChild($toProp) | Out-Null

    $xml.DocumentElement.AppendChild($propertyGroup) | Out-Null

    Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $ConsumerCsprojPath
}
