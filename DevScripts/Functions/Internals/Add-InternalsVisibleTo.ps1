# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Add-InternalsVisibleTo {
    <#
    .SYNOPSIS
    Adds an InternalsVisibleTo entry to a .csproj file
    
    .DESCRIPTION
    Adds an InternalsVisibleTo element to a .csproj file if it doesn't already exist
    
    .PARAMETER CsprojPath
    Path to the .csproj file
    
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
    
    if (-not (Test-Path $CsprojPath)) {
        Write-Error "Project file not found: $CsprojPath"
        return
    }
    
    [xml]$xml = Get-Content $CsprojPath
    
    # Check if InternalsVisibleTo already exists
    $existingIVT = $xml.SelectNodes("//InternalsVisibleTo[@Include]") | 
        Where-Object { $_.GetAttribute("Include") -eq $AssemblyName } | 
        Select-Object -First 1
    
    if ($existingIVT) {
        return # Already exists
    }
    
    # Find or create ItemGroup for InternalsVisibleTo
    $itemGroup = $xml.SelectSingleNode("//ItemGroup[InternalsVisibleTo]")
    
    if (-not $itemGroup) {
        # Create a new ItemGroup
        $itemGroup = $xml.CreateElement("ItemGroup")
        $xml.DocumentElement.AppendChild($itemGroup) | Out-Null
    }
    
    # Create and add the InternalsVisibleTo element
    $ivt = $xml.CreateElement("InternalsVisibleTo")
    $ivt.SetAttribute("Include", $AssemblyName)
    $itemGroup.AppendChild($ivt) | Out-Null
    
    $xml.Save($CsprojPath)
}
