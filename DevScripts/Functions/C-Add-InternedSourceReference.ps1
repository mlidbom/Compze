# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Add-InternedSourceReference {
    <#
    .SYNOPSIS
    Configures a project to internalize source from another project directory

    .DESCRIPTION
    Adds the CircularLibraryDependencySourceRewriter .targets import, a ProjectReference
    to CircularLibraryDependencySourceRewriter, and sets InternalizeSourceFrom/InternalizeSourceTo
    properties in the consumer project. This allows the consumer to compile an internal copy of
    the source project's code, which is useful for resolving circular dependency scenarios.

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

    $cldrProjectPath = Join-Path $script:CompzeRoot "CircularLibraryDependencySourceRewriter" "src" "CircularLibraryDependencySourceRewriter" "CircularLibraryDependencySourceRewriter.csproj"
    $cldrProjectRelativePath = [System.IO.Path]::GetRelativePath($consumerDir, $cldrProjectPath)

    $targetsAbsolutePath = Join-Path $script:CompzeRoot "CircularLibraryDependencySourceRewriter" "src" "CircularLibraryDependencySourceRewriter" "CircularLibraryDependencySourceRewriter.targets"
    $targetsRelativePath = [System.IO.Path]::GetRelativePath($consumerDir, $targetsAbsolutePath)
    $sourceRelativePath = [System.IO.Path]::GetRelativePath($consumerDir, $SourceProjectDir)

    # Add ProjectReference to CircularLibraryDependencySourceRewriter (does its own file read/write)
    Add-ProjectReference -CsprojPath $ConsumerCsprojPath -ReferencePath $cldrProjectRelativePath

    # Now load the file (after Add-ProjectReference may have modified it) for remaining changes
    [xml]$xml = Get-Content $ConsumerCsprojPath

    # Add Import for the .targets file if not already present
    $existingImport = $xml.SelectNodes("//Import[@Project]") |
        Where-Object { $_.GetAttribute("Project") -like "*CircularLibraryDependencySourceRewriter.targets" } |
        Select-Object -First 1

    if (-not $existingImport) {
        $import = $xml.CreateElement("Import")
        $import.SetAttribute("Project", $targetsRelativePath)
        $xml.DocumentElement.AppendChild($import) | Out-Null
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
