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
    2. Copies all ProjectReference entries from the source to the split project
    3. For each project that has a ProjectReference to the source, adds a ProjectReference to the split project
    4. Optionally adds a reference between the source and split projects (via switches)

    .PARAMETER SourceProject
    The name of the source project to split from (e.g., "Compze.Wiring")

    .PARAMETER SplitProject
    The name of the new project to create (e.g., "Compze.Wiring.Testing")

    .PARAMETER SplitProjectReferencesSourceProject
    When set, adds a ProjectReference from the split project to the source project.
    Use when the extracted code depends on what remains in the source.
    Mutually exclusive with -SourceProjectReferencesSplitProject and -UseInternedSourceReferences.

    .PARAMETER SourceProjectReferencesSplitProject
    When set, adds a ProjectReference from the source project to the split project.
    Use when the remaining code depends on what was extracted.
    Mutually exclusive with -SplitProjectReferencesSourceProject and -UseInternedSourceReferences.

    .PARAMETER UseInternedSourceReferences
    When set, instead of a normal ProjectReference, configures the source project to internalize
    the split project's source code using CircularLibraryDependencySourceRewriter. This adds an
    Import of the .targets file and sets InternalizeSourceFrom/InternalizeSourceTo properties.
    Also adds a normal ProjectReference from the split project back to the source project.
    Use when both projects need each other's code (circular dependency scenario).
    Mutually exclusive with -SplitProjectReferencesSourceProject and -SourceProjectReferencesSplitProject.

    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Wiring -SplitProject Compze.Wiring.Testing
    Creates Compze.Wiring.Testing with all references inherited from Compze.Wiring, no reference between them.

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Utilities -SplitProject Compze.Utilities.DependencyInjection -SplitProjectReferencesSourceProject
    Creates Compze.Utilities.DependencyInjection with a reference back to Compze.Utilities.

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Core -SplitProject Compze.Core.Abstractions -SourceProjectReferencesSplitProject
    Creates Compze.Core.Abstractions and adds a reference from Compze.Core to it.

    .EXAMPLE
    C-Split-Project -SourceProject Compze.Tessaging -SplitProject Compze.Tessaging.Internals -UseInternedSourceReferences
    Creates Compze.Tessaging.Internals. The split project gets a normal reference to the source.
    The source project internalizes the split project's source via CircularLibraryDependencySourceRewriter.
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SourceProject,

        [Parameter(Mandatory = $true)]
        [string]$SplitProject,

        [switch]$SplitProjectReferencesSourceProject,

        [switch]$SourceProjectReferencesSplitProject,

        [switch]$UseInternedSourceReferences,

        [string]$SolutionPath
    )

    # Validate mutual exclusivity
    $switchCount = 0
    if ($SplitProjectReferencesSourceProject) { $switchCount++ }
    if ($SourceProjectReferencesSplitProject) { $switchCount++ }
    if ($UseInternedSourceReferences) { $switchCount++ }
    if ($switchCount -gt 1) {
        Write-Error "-SplitProjectReferencesSourceProject, -SourceProjectReferencesSplitProject, and -UseInternedSourceReferences are mutually exclusive"
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

    # Step 3: Copy all project references from source to split project
    $sourceReferences = Get-ProjectReferences -CsprojPath $sourceProjectFile.FullName

    if ($sourceReferences -and $sourceReferences.Count -gt 0) {
        foreach ($reference in $sourceReferences) {
            $absoluteReferencePath = [System.IO.Path]::GetFullPath((Join-Path $sourceProjectDir $reference))
            $newRelativePath = [System.IO.Path]::GetRelativePath($splitProjectDir, $absoluteReferencePath)
            Add-ProjectReference -CsprojPath $splitProjectFile.FullName -ReferencePath $newRelativePath
        }
    }

    # Step 4: For each project that references the source, add a reference to the split project
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

    # Step 5: Add reference between source and split projects based on switches
    if ($SplitProjectReferencesSourceProject) {
        $relPath = [System.IO.Path]::GetRelativePath($splitProjectDir, $sourceProjectFile.FullName)
        Add-ProjectReference -CsprojPath $splitProjectFile.FullName -ReferencePath $relPath

    } elseif ($SourceProjectReferencesSplitProject) {
        $relPath = [System.IO.Path]::GetRelativePath($sourceProjectDir, $splitProjectFile.FullName)
        Add-ProjectReference -CsprojPath $sourceProjectFile.FullName -ReferencePath $relPath

    } elseif ($UseInternedSourceReferences) {
        # The split project gets a normal reference to the source project
        $splitToSourcePath = [System.IO.Path]::GetRelativePath($splitProjectDir, $sourceProjectFile.FullName)
        Add-ProjectReference -CsprojPath $splitProjectFile.FullName -ReferencePath $splitToSourcePath

        # The source project internalizes source from the split project via CircularLibraryDependencySourceRewriter
        $targetsAbsolutePath = Join-Path $script:CompzeRoot "CircularLibraryDependencySourceRewriter" "src" "CircularLibraryDependencySourceRewriter" "CircularLibraryDependencySourceRewriter.targets"
        $targetsRelativePath = [System.IO.Path]::GetRelativePath($sourceProjectDir, $targetsAbsolutePath)

        $splitProjectDirRelative = [System.IO.Path]::GetRelativePath($sourceProjectDir, $splitProjectDir)

        [xml]$xml = Get-Content $sourceProjectFile.FullName

        # Add Import for the .targets file if not already present
        $existingImport = $xml.SelectNodes("//Import[@Project]") |
            Where-Object { $_.GetAttribute("Project") -like "*CircularLibraryDependencySourceRewriter.targets" } |
            Select-Object -First 1

        if (-not $existingImport) {
            $import = $xml.CreateElement("Import")
            $import.SetAttribute("Project", $targetsRelativePath)
            $xml.DocumentElement.AppendChild($import) | Out-Null
        }

        # Add or update PropertyGroup with InternalizeSourceFrom/To
        $propertyGroup = $xml.CreateElement("PropertyGroup")

        $fromProp = $xml.CreateElement("InternalizeSourceFrom")
        $fromProp.InnerText = $splitProjectDirRelative
        $propertyGroup.AppendChild($fromProp) | Out-Null

        $toProp = $xml.CreateElement("InternalizeSourceTo")
        $toProp.InnerText = '$(MSBuildProjectDirectory)\InternalizedSource'
        $propertyGroup.AppendChild($toProp) | Out-Null

        $xml.DocumentElement.AppendChild($propertyGroup) | Out-Null

        Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $sourceProjectFile.FullName
    }
}
