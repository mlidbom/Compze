# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Merge-Project {
    <#
    .SYNOPSIS
    Merges a project back into its parent project
    
    .DESCRIPTION
    Performs the inverse operation of C-Split-Project by merging a child project back into its parent.
    This is useful for consolidating projects that were split but are no longer needed separately.
    
    The command performs the following operations:
    1. Identifies the parent project based on the project path (e.g., Compze.Wiring.Testing -> Compze.Wiring)
    2. Removes the project from all .slnx solution files
    3. Deletes the .csproj file
    4. Deletes obj and bin folders from the project directory
    5. Updates all projects that reference the child project:
       - Removes the reference to the child project
       - Adds a reference to the parent project if not already present
    6. Runs C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders to clean up exclusions
    
    .PARAMETER Project
    The name of the project to merge back into its parent (e.g., "Compze.Wiring.Testing")
    
    .PARAMETER SolutionPath
    Path to the main solution file (defaults to Compze.AllProjects.slnx)
    
    .EXAMPLE
    C-Merge-Project -Project Compze.Wiring.Testing
    Merges Compze.Wiring.Testing back into Compze.Wiring
    
    .EXAMPLE
    C-Merge-Project -Project Compze.Common.Internal
    Merges Compze.Common.Internal back into Compze.Common
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,
        
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
    
    # Step 1: Find the project to merge
    $projectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Project
    
    if (-not $projectFile) {
        Write-Error "Project not found: $Project"
        return
    }
    
    # Step 2: Determine the parent project name by walking up the hierarchy
    # Compze.Utilities.Threading.Testing -> try Compze.Utilities.Threading, then Compze.Utilities, etc.
    $projectParts = $Project -split '\.'
    if ($projectParts.Length -lt 2) {
        Write-Error "Cannot determine parent project for: $Project (project name must have at least two parts)"
        return
    }
    
    $parentProjectName = $null
    $parentProjectFile = $null
    
    # Start from the immediate parent and walk up until we find an existing project
    for ($i = $projectParts.Length - 2; $i -gt 0; $i--) {
        $candidateParentName = ($projectParts[0..$i]) -join '.'
        $candidateParentFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $candidateParentName
        
        if ($candidateParentFile) {
            $parentProjectName = $candidateParentName
            $parentProjectFile = $candidateParentFile
            break
        }
    }
    
    if (-not $parentProjectFile) {
        Write-Error "No parent project found in hierarchy for: $Project"
        return
    }
    
    $projectDir = Split-Path -Parent $projectFile.FullName
    $projectCsprojPath = $projectFile.FullName
    
    # Step 3: Remove the project from all .slnx solution files
    $slnxFiles = Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse
    
    foreach ($slnxFile in $slnxFiles) {
        Remove-ProjectFromSolution -ProjectName $Project -SolutionPath $slnxFile.FullName
    }
    
    # Step 4: Update all projects that reference this project
    $allProjects = Get-AllProjectFiles -SolutionPath $SolutionPath
    $referencesUpdated = 0
    
    foreach ($referencingProject in $allProjects) {
        # Skip the project being merged and its parent
        if ($referencingProject.BaseName -eq $Project -or $referencingProject.BaseName -eq $parentProjectName) {
            continue
        }
        
        $projectReferences = Get-ProjectReferences -CsprojPath $referencingProject.FullName
        
        # Check if this project references the one being merged
        $referencesChild = $false
        foreach ($ref in $projectReferences) {
            if ($ref -like "*$Project.csproj") {
                $referencesChild = $true
                break
            }
        }
        
        if ($referencesChild) {
            # Remove reference to the child project
            Remove-ProjectReference -CsprojPath $referencingProject.FullName -ReferencePath "$Project.csproj"
            
            # Check if it already references the parent
            $referencesParent = $false
            foreach ($ref in $projectReferences) {
                if ($ref -like "*$parentProjectName.csproj") {
                    $referencesParent = $true
                    break
                }
            }
            
            # Add reference to parent if not already present
            if (-not $referencesParent) {
                $referencingProjectDir = Split-Path -Parent $referencingProject.FullName
                $parentProjectPath = $parentProjectFile.FullName
                $relativePathToParent = [System.IO.Path]::GetRelativePath($referencingProjectDir, $parentProjectPath)
                
                Add-ProjectReference -CsprojPath $referencingProject.FullName -ReferencePath $relativePathToParent
            }
            
            $referencesUpdated++
        }
    }
    
    # Step 5: Delete the .csproj file
    if (Test-Path $projectCsprojPath) {
        Remove-Item -Path $projectCsprojPath -Force
    }
    
    # Step 6: Delete obj and bin folders
    $objFolder = Join-Path $projectDir "obj"
    $binFolder = Join-Path $projectDir "bin"
    
    if (Test-Path $objFolder) {
        Remove-Item -Path $objFolder -Recurse -Force
    }
    
    if (Test-Path $binFolder) {
        Remove-Item -Path $binFolder -Recurse -Force
    }
    
    # Step 7: Run C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
    # This will clean up any exclusions in the parent project that are no longer needed
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
    
    # This prevents duplicate assembly attribute errors from stale obj/bin folders
    C-Clean
}
