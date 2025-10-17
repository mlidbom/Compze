function Add-ProjectReference {
    <#
    .SYNOPSIS
    Adds a ProjectReference to a .csproj file
    
    .DESCRIPTION
    Adds a ProjectReference element to a .csproj file if it doesn't already exist
    
    .PARAMETER CsprojPath
    Path to the .csproj file
    
    .PARAMETER ReferencePath
    Relative path to the referenced project (e.g., "..\Other\Project.csproj")
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$CsprojPath,
        
        [Parameter(Mandatory = $true)]
        [string]$ReferencePath
    )
    
    if (-not (Test-Path $CsprojPath)) {
        Write-Error "Project file not found: $CsprojPath"
        return
    }
    
    [xml]$xml = Get-Content $CsprojPath
    
    # Check if reference already exists
    $existingRef = $xml.SelectNodes("//ProjectReference[@Include]") | 
        Where-Object { $_.GetAttribute("Include") -eq $ReferencePath } | 
        Select-Object -First 1
    
    if ($existingRef) {
        return # Already exists
    }
    
    # Find or create ItemGroup for ProjectReferences
    $itemGroup = $xml.SelectSingleNode("//ItemGroup[ProjectReference]")
    
    if (-not $itemGroup) {
        # Create a new ItemGroup
        $itemGroup = $xml.CreateElement("ItemGroup")
        $xml.DocumentElement.AppendChild($itemGroup) | Out-Null
    }
    
    # Create and add the ProjectReference element
    $projectRef = $xml.CreateElement("ProjectReference")
    $projectRef.SetAttribute("Include", $ReferencePath)
    $itemGroup.AppendChild($projectRef) | Out-Null
    
    $xml.Save($CsprojPath)
}
