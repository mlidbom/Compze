# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Add-InternalsVisibleTo {
    <#
    .SYNOPSIS
    Adds an InternalsVisibleTo entry to the centralized Directory.Build.props file
    
    .DESCRIPTION
    Adds an InternalsVisibleTo element to src/Compze/Directory.Build.props if it doesn't already exist.
    For now at least, we make the internals of every Compze.* assembly visible to every other Compze.* assembly through this shared file.
    
    .PARAMETER CsprojPath
    Path to a .csproj file (used to locate the Directory.Build.props file, parameter kept for compatibility)
    
    .PARAMETER AssemblyName
    Name of the assembly to grant InternalsVisibleTo access
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath,
        
        [Parameter(Mandatory = $true)]
        [string]$AssemblyName
    )
    
    # Locate the centralized Directory.Build.props file
    $directoryBuildPropsPath = Join-Path (Split-Path $PSScriptRoot -Parent) "..\src\Compze\Directory.Build.props" | Resolve-Path
    
    if (-not (Test-Path $directoryBuildPropsPath)) {
        Write-Error "Directory.Build.props not found at: $directoryBuildPropsPath"
        return
    }
    
    [xml]$xml = Get-Content $directoryBuildPropsPath
    
    # Check if InternalsVisibleTo already exists
    $existingIVT = $xml.SelectNodes("//InternalsVisibleTo[@Include]") | 
        Where-Object { $_.GetAttribute("Include") -eq $AssemblyName } | 
        Select-Object -First 1
    
    if ($existingIVT) {
        return # Already exists
    }
    
    # Find the ItemGroup that contains InternalsVisibleTo entries
    $itemGroup = $xml.SelectSingleNode("//ItemGroup[InternalsVisibleTo]")
    
    if (-not $itemGroup) {
        Write-Error "InternalsVisibleTo ItemGroup not found in Directory.Build.props"
        return
    }
    
    # Create and add the InternalsVisibleTo element
    $ivt = $xml.CreateElement("InternalsVisibleTo")
    $ivt.SetAttribute("Include", $AssemblyName)
    
    # Insert in alphabetical order
    $inserted = $false
    foreach ($child in $itemGroup.ChildNodes) {
        if ($child.Name -eq "InternalsVisibleTo") {
            $existingName = $child.GetAttribute("Include")
            if ([string]::Compare($AssemblyName, $existingName, [System.StringComparison]::OrdinalIgnoreCase) -lt 0) {
                $itemGroup.InsertBefore($ivt, $child) | Out-Null
                $inserted = $true
                break
            }
        }
    }
    
    if (-not $inserted) {
        $itemGroup.AppendChild($ivt) | Out-Null
    }
    
    Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $directoryBuildPropsPath
}
