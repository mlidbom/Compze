# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Remove-ProjectReference {
    <#
    .SYNOPSIS
    Removes a ProjectReference from a .csproj file
    
    .DESCRIPTION
    Removes a ProjectReference element from a .csproj file if it exists.
    The reference can be matched by exact path or by project name.
    When the reference is the project half of a FlexRef pair, the package half and the pair's comment go too:
    C-FlexRef-Sync leaves a package half it finds alone, guarded by a property nothing sets any more.

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
    $flexRefPairCondition = [regex]::Match($itemGroup.GetAttribute("Condition"), "^'\`$\((UsePackageReference_[A-Za-z0-9_]+)\)' != 'true'$")
    $itemGroup.RemoveChild($refToRemove) | Out-Null

    # If ItemGroup is now empty, remove it too
    if ($itemGroup.ChildNodes.Count -eq 0) {
        $itemGroup.ParentNode.RemoveChild($itemGroup) | Out-Null
    }

    if ($flexRefPairCondition.Success) {
        Remove-PackageHalfOfFlexRefPair -Xml $xml -FlexRefProperty $flexRefPairCondition.Groups[1].Value
    }

    Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $CsprojPath
}

function Remove-PackageHalfOfFlexRefPair {
    # The package half of a FlexRef pair: the ItemGroup that holds the PackageReference when the property is true,
    # and the comment before it that names the pair, "<!-- Compze.X -- flex reference -->".
    param([xml]$Xml, [string]$FlexRefProperty)

    $packageHalfCondition = "'`$($FlexRefProperty)' == 'true'"
    $packageHalves = @($Xml.SelectNodes("//ItemGroup[@Condition]") | Where-Object { $_.GetAttribute("Condition") -eq $packageHalfCondition })
    foreach ($packageHalf in $packageHalves) {
        $comment = $packageHalf.PreviousSibling
        if ($comment -is [System.Xml.XmlComment] -and $comment.Value -like "*flex reference*") {
            $comment.ParentNode.RemoveChild($comment) | Out-Null
        }
        $packageHalf.ParentNode.RemoveChild($packageHalf) | Out-Null
    }
}
