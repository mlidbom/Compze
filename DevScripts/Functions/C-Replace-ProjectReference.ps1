# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Replace-ProjectReference {
    <#
    .SYNOPSIS
    Replaces all references to one project with references to another project
    
    .DESCRIPTION
    Finds all projects that reference the old project and replaces those references
    with references to the new project. If a project already references the new project,
    it won't create a duplicate reference.
    
    .PARAMETER Old
    The name of the project reference to replace (e.g., "Compze.Wiring.Testing")
    
    .PARAMETER New
    The name of the project to reference instead (e.g., "Compze.Wiring")
    
    .PARAMETER SolutionPath
    Path to the main solution file (defaults to src\Compze.AllProjects.slnx)
    
    .EXAMPLE
    C-Replace-ProjectReference -Old Compze.Wiring.Testing -New Compze.Wiring
    Replaces all references to Compze.Wiring.Testing with Compze.Wiring
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Old,
        
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
    
    # Verify the old project exists
    $oldProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Old
    
    if (-not $oldProjectFile) {
        Write-Error "Old project not found: $Old"
        return
    }
    
    # Verify the new project exists
    $newProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $New
    
    if (-not $newProjectFile) {
        Write-Error "New project not found: $New"
        return
    }
    
    # Get all projects in the solution
    $allProjects = Get-AllProjectFiles -SolutionPath $SolutionPath
    $replacedCount = 0
    
    foreach ($project in $allProjects) {
        # Skip the old and new projects themselves
        if ($project.BaseName -eq $Old -or $project.BaseName -eq $New) {
            continue
        }
        
        $projectReferences = Get-ProjectReferences -CsprojPath $project.FullName
        
        # Check if this project references the old project
        $referencesOld = $false
        foreach ($ref in $projectReferences) {
            if ($ref -like "*$Old.csproj") {
                $referencesOld = $true
                break
            }
        }
        
        if (-not $referencesOld) {
            continue
        }
        
        # Remove the old reference
        Remove-ProjectReference -CsprojPath $project.FullName -ReferencePath "$Old.csproj"
        
        # Check if it already references the new project
        $referencesNew = $false
        foreach ($ref in $projectReferences) {
            if ($ref -like "*$New.csproj") {
                $referencesNew = $true
                break
            }
        }
        
        # Add the new reference if not already present
        if (-not $referencesNew) {
            $projectDir = Split-Path -Parent $project.FullName
            $newProjectPath = $newProjectFile.FullName
            $relativePathToNew = [System.IO.Path]::GetRelativePath($projectDir, $newProjectPath)
            
            Add-ProjectReference -CsprojPath $project.FullName -ReferencePath $relativePathToNew
        }
        
        $replacedCount++
    }
}
