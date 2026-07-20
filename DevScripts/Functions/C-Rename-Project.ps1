# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Rename-Project {
    <#
    .SYNOPSIS
    Renames a project, its directory, and every reference to it

    .DESCRIPTION
    Renames a project throughout the solution:
    - Renames the project directory, so that it keeps matching the project name (src/<ProjectName>/<ProjectName>.csproj)
    - Renames the project file (.csproj)
    - Renames any associated .ncrunchproject files
    - Updates every path pointing at the project file, in all .csproj files and in all solution files (.slnx and .sln)
    - Updates InternalsVisibleTo elements naming the project's assembly
    - Runs C-FlexRef-Sync, so that the FlexRef properties, conditions and package references follow the new name

    The script will search for and update ALL solution files found in the solution
    directory, not just a single file.

    Namespaces are deliberately left alone: a project's namespaces are not required to match its name, and
    renaming them is a source-level refactoring for the IDE, which also updates using directives and doc comments.

    .PARAMETER Old
    The current name of the project to rename (e.g., "Compze.Tessaging.Hosting.Configuration")

    .PARAMETER New
    The new name for the project (e.g., "Compze.Common.Configuration")

    .PARAMETER SolutionPath
    Path to the solution file (defaults to Compze.AllProjects.slnx)

    .EXAMPLE
    C-Rename-Project -Old Compze.Tessaging.Hosting.Configuration -New Compze.Common.Configuration
    Renames src/Compze.Tessaging.Hosting.Configuration/Compze.Tessaging.Hosting.Configuration.csproj
    to src/Compze.Common.Configuration/Compze.Common.Configuration.csproj and updates all references to it

    .EXAMPLE
    C-Rename-Project -Old Compze.Old.Name -New Compze.New.Name -SolutionPath "MySolution.slnx"
    Renames the project using a custom solution path
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

    $solutionDir = Split-Path -Parent $SolutionPath

    # Step 1: Find the project file and work out where it is to end up
    $projectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Old

    if (-not $projectFile) {
        Write-Error "Project file '$Old.csproj' not found in solution directory"
        return
    }

    $oldProjectDirectory = $projectFile.Directory

    # A project directory is normally named after the single project it holds, and has to keep doing so - that is
    # what C-Validate-SolutionStructure enforces. The exempted directories (src/SolutionStructure, src/msbuild)
    # hold several projects each, so there the directory name is not this project's to rename.
    $projectDirectoryIsNamedAfterTheProject = $oldProjectDirectory.Name -eq $Old
    $newProjectDirectory = if ($projectDirectoryIsNamedAfterTheProject) { Join-Path $oldProjectDirectory.Parent.FullName $New } else { $oldProjectDirectory.FullName }

    # Step 2: Refuse to overwrite anything that already carries the new name
    if ($projectDirectoryIsNamedAfterTheProject -and (Test-Path $newProjectDirectory)) {
        Write-Error "Target project directory already exists: $newProjectDirectory"
        return
    }

    if (Test-Path (Join-Path $oldProjectDirectory.FullName "$New.csproj")) {
        Write-Error "Target project file already exists: $(Join-Path $newProjectDirectory "$New.csproj")"
        return
    }

    # Step 3: Clean before moving anything. Build artifacts left under the old name would otherwise be dragged
    # along into the renamed directory, and open handles into obj/ can make renaming the directory fail.
    C-Clean

    # Step 4: Rename the directory, the project file, and any associated .ncrunchproject files
    if ($projectDirectoryIsNamedAfterTheProject) {
        Rename-Item -Path $oldProjectDirectory.FullName -NewName $New -ErrorAction Stop
    }

    Rename-Item -Path (Join-Path $newProjectDirectory "$Old.csproj") -NewName "$New.csproj" -ErrorAction Stop

    foreach ($ncrunchProjectFile in Get-ChildItem -Path $newProjectDirectory -Filter "$Old.*.ncrunchproject") {
        $ncrunchProjectExtension = $ncrunchProjectFile.Name.Substring($Old.Length) # e.g., ".v3.ncrunchproject"
        Rename-Item -Path $ncrunchProjectFile.FullName -NewName "$New$ncrunchProjectExtension" -ErrorAction Stop
    }

    # Step 5: Update every reference to the project: the paths in ProjectReference elements of .csproj files and in
    # Project Path elements of solution files, and the assembly name in InternalsVisibleTo elements, which names the
    # project rather than pointing at it and so is not covered by the path rewrite. Both the directory segment and
    # the file name change, and solution files use forward slashes where .csproj files use backslashes, so the
    # separators are kept as they are found.
    $oldProjectPath = '(["/\\])' + [regex]::Escape($oldProjectDirectory.Name) + '([/\\])' + [regex]::Escape($Old) + '\.csproj'
    $newProjectPath = '${1}' + (Split-Path -Leaf $newProjectDirectory) + '${2}' + $New + '.csproj'

    $oldAssemblyName = '(<InternalsVisibleTo\s+Include=")' + [regex]::Escape($Old) + '(")'
    $newAssemblyName = '${1}' + $New + '${2}'

    $filesThatCanReferenceTheProject = @(Get-AllProjectFiles -SolutionPath $SolutionPath) +
                                       @(Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse) +
                                       @(Get-ChildItem -Path $solutionDir -Filter "*.sln" -Recurse)

    foreach ($file in $filesThatCanReferenceTheProject) {
        $content = Get-Content $file.FullName -Raw
        $updatedContent = $content -replace $oldProjectPath, $newProjectPath -replace $oldAssemblyName, $newAssemblyName

        if ($updatedContent -ne $content) {
            Set-Content -Path $file.FullName -Value $updatedContent -NoNewline -Encoding UTF8
        }
    }

    # Step 6: The FlexRef infrastructure names projects rather than pointing at their paths: the
    # UsePackageReference_<Project> properties in Directory.Build.props, the conditions and PackageReference
    # elements they guard in every referencing .csproj, and the NCrunch solution files. Syncing regenerates
    # them all from the renamed project, which nothing above can do.
    C-FlexRef-Sync | Out-Null
}
