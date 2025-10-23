# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Remove-ProjectFromSolution {
    <#
    .SYNOPSIS
    Removes a project from the solution file
    
    .DESCRIPTION
    Removes a Project element from the solution XML file if it exists.
    Also cleans up empty folders.
    
    .PARAMETER ProjectName
    The name of the project to remove from the solution
    
    .PARAMETER SolutionPath
    Path to the solution file
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }
    
    [xml]$xml = Get-Content $SolutionPath
    
    # Calculate the expected project path
    $solutionProjectPath = $ProjectName -replace '\.', '/'
    $solutionProjectPath = "$solutionProjectPath/$ProjectName.csproj"
    
    # Find the project element
    $projectElement = $xml.SelectNodes("//Project[@Path]") | 
        Where-Object { $_.Path -eq $solutionProjectPath } | 
        Select-Object -First 1
    
    if (-not $projectElement) {
        return # Project not in solution
    }
    
    # Remove the project
    $parentFolder = $projectElement.ParentNode
    $parentFolder.RemoveChild($projectElement) | Out-Null
    
    # Clean up empty folders recursively
    while ($parentFolder -and $parentFolder.Name -eq "Folder") {
        # Check if folder is now empty (no Project or Folder children)
        $hasChildren = ($parentFolder.SelectNodes("Project").Count -gt 0) -or 
                       ($parentFolder.SelectNodes("Folder").Count -gt 0)
        
        if (-not $hasChildren) {
            $grandparent = $parentFolder.ParentNode
            $grandparent.RemoveChild($parentFolder) | Out-Null
            $parentFolder = $grandparent
        } else {
            break
        }
    }
    
    $xml.Save($SolutionPath)
}
