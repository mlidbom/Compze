# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Delete-Project {
    <#
    .SYNOPSIS
    Completely deletes a project from the solution
    
    .DESCRIPTION
    Removes a project completely from the codebase, including:
    1. Removes the project from all .slnx solution files
    2. Removes all references to the project from other projects (or replaces them if -ReplaceReferencesWith is specified)
    3. Deletes the entire project directory
    4. Runs C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders to clean up exclusions
    
    WARNING: This is a destructive operation that deletes all files in the project directory.
    
    .PARAMETER Project
    The name of the project to delete (e.g., "Compze.Wiring.Testing")
    
    .PARAMETER ReplaceReferencesWith
    Optional: Instead of removing references, replace them with references to this project
    
    .PARAMETER SolutionPath
    Path to the main solution file (defaults to Compze.AllProjects.slnx)
    
    .EXAMPLE
    C-Delete-Project -Project Compze.Wiring.Testing
    Completely removes the Compze.Wiring.Testing project and removes all references to it
    
    .EXAMPLE
    C-Delete-Project -Project Compze.Wiring.Testing -ReplaceReferencesWith Compze.Wiring
    Completely removes the Compze.Wiring.Testing project and replaces all references with Compze.Wiring
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,
        
        [string]$ReplaceReferencesWith,
        
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
    
    # Step 1: Find the project to delete
    $projectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Project
    
    if (-not $projectFile) {
        Write-Error "Project not found: $Project"
        return
    }
    
    $projectDir = Split-Path -Parent $projectFile.FullName
    
    # Step 2: Remove the project from all .slnx solution files
    $slnxFiles = Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse
    
    foreach ($slnxFile in $slnxFiles) {
        Remove-ProjectFromSolution -ProjectName $Project -SolutionPath $slnxFile.FullName
    }
    
    # Step 3: Handle project references
    if ($ReplaceReferencesWith) {
        # Replace references to the deleted project with references to another project
        C-Replace-ProjectReference -Old $Project -New $ReplaceReferencesWith -SolutionPath $SolutionPath
    } else {
        # Remove all references to this project from other projects
        $allProjects = Get-AllProjectFiles -SolutionPath $SolutionPath
        
        foreach ($referencingProject in $allProjects) {
            # Skip the project being deleted
            if ($referencingProject.BaseName -eq $Project) {
                continue
            }
            
            $projectReferences = Get-ProjectReferences -CsprojPath $referencingProject.FullName
            
            # Check if this project references the one being deleted
            $referencesProject = $false
            foreach ($ref in $projectReferences) {
                if ($ref -like "*$Project.csproj") {
                    $referencesProject = $true
                    break
                }
            }
            
            if ($referencesProject) {
                Remove-ProjectReference -CsprojPath $referencingProject.FullName -ReferencePath "$Project.csproj"
            }
        }
    }
    
    # Step 4: Delete the entire project directory
    if (Test-Path $projectDir) {
        Remove-Item -Path $projectDir -Recurse -Force
    }
    
    # Step 5: Run C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
    # This will clean up any exclusions in parent projects that are no longer needed
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
    
    # Step 6: Run C-Clean to remove all build artifacts
    # This prevents duplicate assembly attribute errors from stale obj/bin folders
    C-Clean
}
