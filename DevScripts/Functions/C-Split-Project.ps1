# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Split-Project {
    <#
    .SYNOPSIS
    Splits a project by creating a new related project with inherited references
    
    .DESCRIPTION
    Creates a new project that inherits the structure and references from a parent project.
    This is useful for creating companion projects like *.Testing or *.Internal projects.
    
    The command performs the following operations:
    1. Creates the new project using C-Create-Project
    2. Copies all ProjectReference entries from the parent to the new project
    3. For each project that has a ProjectReference to the parent, adds a ProjectReference to the new project
    
    .PARAMETER Parent
    The name of the parent project (e.g., "Compze.Wiring")
    
    .PARAMETER New
    The name of the new project to create (e.g., "Compze.Wiring.Testing")
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)
    
    .EXAMPLE
    C-Split-Project -Parent Compze.Wiring -New Compze.Wiring.Testing
    Creates Compze.Wiring.Testing with all references inherited from Compze.Wiring
    
    .EXAMPLE
    C-Split-Project -Parent Compze.Common -New Compze.Common.Internal
    Creates an internal companion project for Compze.Common
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Parent,
        
        [Parameter(Mandatory = $true)]
        [string]$New,
        
        [string]$SolutionPath
    )
    
    # Set default solution path if not provided
    if (-not $SolutionPath) {
        $SolutionPath = $script:CompzeSolutionPath
    }
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }
    
    $solutionDir = Split-Path -Parent $SolutionPath
    
    # Step 1: Find the parent project
    $parentProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Parent
    
    if (-not $parentProjectFile) {
        Write-Error "Parent project not found: $Parent"
        return
    }
    
    # Step 2: Create the new project
    C-Create-Project -ProjectName $New
    
    # Verify the new project was created
    $newProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $New
    
    if (-not $newProjectFile) {
        Write-Error "Failed to create new project: $New"
        return
    }
    
    # Step 3: Copy all project references from parent to new project
    $parentReferences = Get-ProjectReferences -CsprojPath $parentProjectFile.FullName
    
    if ($parentReferences -and $parentReferences.Count -gt 0) {
        $parentProjectDir = Split-Path -Parent $parentProjectFile.FullName
        $newProjectDir = Split-Path -Parent $newProjectFile.FullName
        
        foreach ($reference in $parentReferences) {
            # Convert the reference path from parent project's perspective to new project's perspective
            # Get absolute path of the referenced project
            $absoluteReferencePath = [System.IO.Path]::GetFullPath((Join-Path $parentProjectDir $reference))
            
            # Calculate relative path from new project to the referenced project
            $newRelativePath = [System.IO.Path]::GetRelativePath($newProjectDir, $absoluteReferencePath)
            
            Add-ProjectReference -CsprojPath $newProjectFile.FullName -ReferencePath $newRelativePath
        }
    }
    
    # Step 4: For each project that references the parent, add a reference to the new project
    $referenceAddedCount = 0
    
    foreach ($project in $allProjects) {
        # Skip the parent and new projects themselves
        if ($project.BaseName -eq $Parent -or $project.BaseName -eq $New) {
            continue
        }
        
        $projectReferences = Get-ProjectReferences -CsprojPath $project.FullName
        
        # Check if this project references the parent
        $referencesParent = $false
        foreach ($ref in $projectReferences) {
            if ($ref -like "*$Parent.csproj") {
                $referencesParent = $true
                break
            }
        }
        
        if ($referencesParent) {
            # Calculate relative path from this project to the new project
            $projectDir = Split-Path -Parent $project.FullName
            $newProjectPath = $newProjectFile.FullName
            $relativePathToNew = [System.IO.Path]::GetRelativePath($projectDir, $newProjectPath)
            
            Add-ProjectReference -CsprojPath $project.FullName -ReferencePath $relativePathToNew
            $referenceAddedCount++
        }
    }
}
