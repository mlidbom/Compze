# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Remove-ProjectReference {
    <#
    .SYNOPSIS
    Removes a ProjectReference from a .csproj file
    
    .DESCRIPTION
    Removes a ProjectReference element from a .csproj file if it exists.
    The reference can be matched by exact path or by project name.
    
    .PARAMETER CsprojPath
    Path to the .csproj file
    
    .PARAMETER ReferencePath
    Relative path to the referenced project (e.g., "..\Other\Project.csproj") or just the project name
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
    
    # Find the reference - match by exact path or by project name
    $projectRefs = $xml.SelectNodes("//ProjectReference[@Include]")
    $refToRemove = $null
    
    foreach ($ref in $projectRefs) {
        $includePath = $ref.GetAttribute("Include")
        # Match by exact path or if the reference ends with the specified project name
        if ($includePath -eq $ReferencePath -or $includePath -like "*$ReferencePath") {
            $refToRemove = $ref
            break
        }
    }
    
    if (-not $refToRemove) {
        return # Reference doesn't exist
    }
    
    # Remove the reference
    $itemGroup = $refToRemove.ParentNode
    $itemGroup.RemoveChild($refToRemove) | Out-Null
    
    # If ItemGroup is now empty, remove it too
    if ($itemGroup.ChildNodes.Count -eq 0) {
        $itemGroup.ParentNode.RemoveChild($itemGroup) | Out-Null
    }
    
    Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $CsprojPath
}
