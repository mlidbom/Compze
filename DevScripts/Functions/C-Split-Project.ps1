# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Split-Project {
    <#
    .SYNOPSIS
    Splits a project by creating a new related project with inherited references

    .DESCRIPTION
    Creates a new project that inherits the structure and references from a source project.
    This is useful for creating companion projects like *.Testing or *.Internal projects.

    The command performs the following operations:
    1. Creates the new project using C-Create-Project
    2. Moves source files from the corresponding subfolder in the source project to the split project
    3. Copies all ProjectReference entries from the source to the split project
    4. For each project that has a ProjectReference to the source, adds a ProjectReference to the split project
    5. Optionally adds references between the source and split projects (via switches)

    Reference switches control two independent directions. Within each direction, normal ProjectReference
    and interned source reference are mutually exclusive, but you can combine switches across directions.

    .PARAMETER SourceProject
    The name of the source project to split from (e.g., "Compze.Wiring")

    .PARAMETER SplitProject
    The name of the new project to create (e.g., "Compze.Wiring.Testing")

    .PARAMETER SplitProjectReferencesSourceProject
    Adds a normal ProjectReference from the split project to the source project.
    Use when the extracted code depends on what remains in the source.
    Mutually exclusive with -SplitProjectSourceReferencesSourceProject.

    .PARAMETER SplitProjectSourceReferencesSourceProject
    Configures the split project to internalize the source project's code using
    CircularLibraryDependencySourceRewriter (imports .targets, sets InternalizeSourceFrom/To).
    Use when the split project needs code from the source but a normal reference would create a cycle.
    Mutually exclusive with -SplitProjectReferencesSourceProject.

    .PARAMETER SourceProjectReferencesSplitProject
    Adds a normal ProjectReference from the source project to the split project.
    Use when the remaining code depends on what was extracted.
    Mutually exclusive with -SourceProjectSourceReferencesSplitProject.

    .PARAMETER SourceProjectSourceReferencesSplitProject
    Configures the source project to internalize the split project's code using
    CircularLibraryDependencySourceRewriter (imports .targets, sets InternalizeSourceFrom/To).
    Use when the source project needs code from the split but a normal reference would create a cycle.
    Mutually exclusive with -SourceProjectReferencesSplitProject.

    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Wiring -SplitProject Compze.Wiring.Testing
    Creates Compze.Wiring.Testing with all references inherited from Compze.Wiring, no reference between them.

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Utilities -SplitProject Compze.Utilities.DependencyInjection -SplitProjectReferencesSourceProject
    Creates Compze.Utilities.DependencyInjection with a normal ProjectReference back to Compze.Utilities.

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Core -SplitProject Compze.Core.Abstractions -SourceProjectReferencesSplitProject
    Creates Compze.Core.Abstractions and adds a normal ProjectReference from Compze.Core to it.

    .EXAMPLE
    C-Split-Project -SourceProject Compze.A -SplitProject Compze.B -SplitProjectReferencesSourceProject -SourceProjectSourceReferencesSplitProject
    Circular dependency: Compze.B has a normal ProjectReference to Compze.A. Compze.A internalizes Compze.B's source.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProject,

        [Parameter(Mandatory = $true)]
        [string]$SplitProject,

        [switch]$SplitProjectReferencesSourceProject,

        [switch]$SplitProjectSourceReferencesSourceProject,

        [switch]$SourceProjectReferencesSplitProject,

        [switch]$SourceProjectSourceReferencesSplitProject,

        [string]$SolutionPath
    )

    # Validate per-direction mutual exclusivity
    if ($SplitProjectReferencesSourceProject -and $SplitProjectSourceReferencesSourceProject) {
        Write-Error "-SplitProjectReferencesSourceProject and -SplitProjectSourceReferencesSourceProject are mutually exclusive (both set the split->source direction)"
        return
    }
    if ($SourceProjectReferencesSplitProject -and $SourceProjectSourceReferencesSplitProject) {
        Write-Error "-SourceProjectReferencesSplitProject and -SourceProjectSourceReferencesSplitProject are mutually exclusive (both set the source->split direction)"
        return
    }

    # Set default solution path if not provided
    if (-not $SolutionPath) {
        $SolutionPath = $script:CompzeSolutionPath
    }

    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return
    }

    # Step 1: Find the source project
    $sourceProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $SourceProject

    if (-not $sourceProjectFile) {
        Write-Error "Source project not found: $SourceProject"
        return
    }

    # Step 2: Create the split project
    C-Create-Project -ProjectName $SplitProject

    # Verify the split project was created
    $splitProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $SplitProject

    if (-not $splitProjectFile) {
        Write-Error "Failed to create split project: $SplitProject"
        return
    }

    $sourceProjectDir = Split-Path -Parent $sourceProjectFile.FullName
    $splitProjectDir = Split-Path -Parent $splitProjectFile.FullName

    # Step 3: Move source files from subfolder in source project to split project
    # Derive subfolder name: Compze.Utilities.DependencyInjection - Compze.Utilities = DependencyInjection
    if ($SplitProject.StartsWith("$SourceProject.")) {
        $subfolderParts = $SplitProject.Substring($SourceProject.Length + 1) -split '\.'
        $subfolderPath = Join-Path $sourceProjectDir ($subfolderParts -join [System.IO.Path]::DirectorySeparatorChar)

        if (Test-Path $subfolderPath) {
            # Move contents into the split project directory
            Get-ChildItem -Path $subfolderPath | Move-Item -Destination $splitProjectDir -Force
            # Remove the now-empty subfolder
            Remove-Item -Path $subfolderPath -Force -Recurse
        }
    }

    # Step 4: Copy all project references from source to split project
    $sourceReferences = Get-ProjectReferences -CsprojPath $sourceProjectFile.FullName

    if ($sourceReferences -and $sourceReferences.Count -gt 0) {
        foreach ($reference in $sourceReferences) {
            $absoluteReferencePath = [System.IO.Path]::GetFullPath((Join-Path $sourceProjectDir $reference))
            $newRelativePath = [System.IO.Path]::GetRelativePath($splitProjectDir, $absoluteReferencePath)
            Add-ProjectReference -CsprojPath $splitProjectFile.FullName -ReferencePath $newRelativePath
        }
    }

    # Step 5: For each project that references the source, add a reference to the split project
    $allProjects = Get-AllProjectFiles -SolutionPath $SolutionPath

    foreach ($project in $allProjects) {
        if ($project.BaseName -eq $SourceProject -or $project.BaseName -eq $SplitProject) {
            continue
        }

        $projectReferences = Get-ProjectReferences -CsprojPath $project.FullName
        $referencesSource = $false
        foreach ($ref in $projectReferences) {
            if ($ref -like "*$SourceProject.csproj") {
                $referencesSource = $true
                break
            }
        }

        if ($referencesSource) {
            $projectDir = Split-Path -Parent $project.FullName
            $relativePathToSplit = [System.IO.Path]::GetRelativePath($projectDir, $splitProjectFile.FullName)
            Add-ProjectReference -CsprojPath $project.FullName -ReferencePath $relativePathToSplit
        }
    }

    # Step 6: Add references between source and split projects based on switches

    # Direction: Split -> Source
    if ($SplitProjectReferencesSourceProject) {
        $relPath = [System.IO.Path]::GetRelativePath($splitProjectDir, $sourceProjectFile.FullName)
        Add-ProjectReference -CsprojPath $splitProjectFile.FullName -ReferencePath $relPath
    } elseif ($SplitProjectSourceReferencesSourceProject) {
        C-Add-InternedSourceReference -ConsumerCsprojPath $splitProjectFile.FullName -SourceProjectDir $sourceProjectDir
    }

    # Direction: Source -> Split
    if ($SourceProjectReferencesSplitProject) {
        $relPath = [System.IO.Path]::GetRelativePath($sourceProjectDir, $splitProjectFile.FullName)
        Add-ProjectReference -CsprojPath $sourceProjectFile.FullName -ReferencePath $relPath
    } elseif ($SourceProjectSourceReferencesSplitProject) {
        C-Add-InternedSourceReference -ConsumerCsprojPath $sourceProjectFile.FullName -SourceProjectDir $splitProjectDir
    }

    # Step 7: Update .csproj exclusions since files may have moved between project directories
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
}
