# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function C-Merge-Project {
    <#
    .SYNOPSIS
    Merges a project into another project, by default its parent

    .DESCRIPTION
    Performs the inverse operation of C-Split-Project by merging a project into another: by default its parent by name,
    the nearest project whose name its name extends (Compze.Wiring.Testing -> Compze.Wiring), or the one -Into names.

    The command performs the following operations:
    1. Identifies the target project: -Into, else the parent by name
    2. Removes the project from all .slnx solution files
    3. Updates all projects that reference the merged project:
       - Removes the reference to the merged project
       - Adds a reference to the target project if not already present
    4. Gives the target project the merged project's references, other than to the target itself: its project
       references, its package references outside the FlexRef pairs, and its InternalsVisibleTo grants. Grants that
       other projects give the merged project's assembly go to the target's instead.
    5. In the flat layout, where the merged project's directory is not inside the target's, moves its files into the
       target's directory, or into -Folder there: all but its project files, its build output, and its CHANGELOG.md
       and README.md, which say what a package was and did, and are left for you to place. It moves nothing that would
       overwrite a file, and refuses before changing anything.
    6. Deletes the .csproj and .ncrunchproject files, and the obj, bin and TestResults folders
    7. Runs C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders to clean up exclusions, and
       C-FlexRef-Sync, so that the FlexRef pairs and the NCrunch solutions follow
    8. Runs C-Clean, so that stale obj and bin folders cause no duplicate assembly attribute errors

    Namespaces are left alone: renaming them is a source-level refactoring for the IDE, which also updates using
    directives and doc comments.

    .PARAMETER Project
    The name of the project to merge (e.g., "Compze.Wiring.Testing")

    .PARAMETER Into
    The name of the project to merge it into. Defaults to its parent by name.

    .PARAMETER Folder
    Where its files go in the flat layout: a folder relative to the target project's directory, created if missing.
    Defaults to the target project's directory itself.

    .PARAMETER SolutionPath
    Path to the main solution file (defaults to Compze.AllProjects.slnx)

    .EXAMPLE
    C-Merge-Project -Project Compze.Wiring.Testing
    Merges Compze.Wiring.Testing back into Compze.Wiring

    .EXAMPLE
    C-Merge-Project -Project Compze.InterprocessObject -Into Compze.Threading.Interprocess -Folder ResourceAccess
    Moves Compze.InterprocessObject's files into src/Compze.Threading.Interprocess/ResourceAccess and merges the
    project into Compze.Threading.Interprocess
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$Project,

        [string]$Into,

        [string]$Folder,

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

    # Step 1: Find the project to merge, and the project to merge it into
    $projectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Project

    if (-not $projectFile) {
        Write-Error "Project not found: $Project"
        return
    }

    if ($Into) {
        $targetProjectName = $Into
        $targetProjectFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $Into
        if (-not $targetProjectFile) {
            Write-Error "Project to merge into not found: $Into"
            return
        }
    }
    else {
        $targetProjectName, $targetProjectFile = Find-ParentProjectByName -SolutionPath $SolutionPath -ProjectName $Project
        if (-not $targetProjectFile) {
            Write-Error "No parent project found in hierarchy for: $Project. Name the project to merge it into with -Into."
            return
        }
    }

    $projectDir = Split-Path -Parent $projectFile.FullName
    $projectCsprojPath = $projectFile.FullName
    $targetDir = Split-Path -Parent $targetProjectFile.FullName

    # Step 2: In the flat layout, decide what moves where, and refuse before changing anything if a move would
    # overwrite a file. In the nested layout the target already holds the merged project's files.
    $filesMove = -not (Test-IsInsideDirectory -Path $projectDir -Directory $targetDir)
    if ($Folder -and -not $filesMove) {
        Write-Error "$Project's directory is inside $targetProjectName's, so its files are already where they belong: -Folder does not apply."
        return
    }

    $destinationDir = if ($Folder) { Join-Path $targetDir $Folder } else { $targetDir }
    $documentsLeftForTheCaller = @('CHANGELOG.md', 'README.md')
    $itemsToMove = @()
    if ($filesMove) {
        $notMoved = @("$Project.csproj", 'bin', 'obj', 'TestResults') + $documentsLeftForTheCaller
        $itemsToMove = @(Get-ChildItem -Path $projectDir -Force | Where-Object {
            $notMoved -notcontains $_.Name -and $_.Name -notlike "$Project.*.ncrunchproject"
        })

        $collisions = @($itemsToMove | Where-Object { Test-Path (Join-Path $destinationDir $_.Name) })
        if ($collisions.Count -gt 0) {
            Write-Error "Moving $Project's files into $destinationDir would overwrite: $(($collisions | ForEach-Object { $_.Name }) -join ', '). Choose another -Folder."
            return
        }
    }

    # Step 3: Remove the project from all .slnx solution files
    $slnxFiles = Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse

    foreach ($slnxFile in $slnxFiles) {
        Remove-ProjectFromSolution -ProjectName $Project -SolutionPath $slnxFile.FullName
    }

    # Step 4: Update all projects that reference this project, and the grants of access to its internals
    $allProjects = Get-AllProjectFiles -SolutionPath $SolutionPath

    foreach ($referencingProject in $allProjects) {
        # Skip the project being merged and its target
        if ($referencingProject.BaseName -eq $Project -or $referencingProject.BaseName -eq $targetProjectName) {
            continue
        }

        Move-InternalsVisibleToGrant -CsprojPath $referencingProject.FullName -From $Project -To $targetProjectName

        $projectReferences = @(Get-ProjectReferences -CsprojPath $referencingProject.FullName)
        if (-not ($projectReferences | Where-Object { $_ -like "*$Project.csproj" })) {
            continue
        }

        Remove-ProjectReference -CsprojPath $referencingProject.FullName -ReferencePath "$Project.csproj"

        if (-not ($projectReferences | Where-Object { $_ -like "*$targetProjectName.csproj" })) {
            $referencingProjectDir = Split-Path -Parent $referencingProject.FullName
            Add-ProjectReference -CsprojPath $referencingProject.FullName -ReferencePath ([System.IO.Path]::GetRelativePath($referencingProjectDir, $targetProjectFile.FullName))
        }
    }

    # Step 5: Give the target the merged project's own references and grants
    Copy-ReferencesToTheProjectItIsMergedInto -CsprojPath $projectCsprojPath -TargetCsprojPath $targetProjectFile.FullName -TargetProjectName $targetProjectName

    # Step 6: Move its files, then delete what is left of the project but the documents that are the caller's to place
    if ($itemsToMove.Count -gt 0) {
        New-Item -ItemType Directory -Path $destinationDir -Force | Out-Null
        foreach ($item in $itemsToMove) {
            Move-Item -LiteralPath $item.FullName -Destination (Join-Path $destinationDir $item.Name) -ErrorAction Stop
        }
    }

    Remove-Item -Path $projectCsprojPath -Force
    Get-ChildItem -Path $projectDir -Filter "$Project.*.ncrunchproject" | Remove-Item -Force

    foreach ($buildOutput in 'obj', 'bin', 'TestResults') {
        $buildOutputFolder = Join-Path $projectDir $buildOutput
        if (Test-Path $buildOutputFolder) {
            Remove-Item -Path $buildOutputFolder -Recurse -Force
        }
    }

    if ($filesMove) {
        $documentsLeft = @($documentsLeftForTheCaller | Where-Object { Test-Path (Join-Path $projectDir $_) })
        if ($documentsLeft.Count -gt 0) {
            Write-Warning "Left $($documentsLeft -join ' and ') in $projectDir`: what $Project was and did is for you to place, such as in $targetProjectName's."
        }
        elseif (-not (Get-ChildItem -Path $projectDir -Force)) {
            Remove-Item -Path $projectDir -Force
        }
    }

    # Step 7: Run C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders
    # This will clean up any exclusions in the target project that are no longer needed
    C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders

    # The FlexRef infrastructure names projects: the UsePackageReference_<Project> properties in Directory.Build.props,
    # the conditions and PackageReference elements they guard in every referencing .csproj, and the NCrunch solution
    # files. Syncing regenerates them all without the merged project, and pairs the references added above.
    C-FlexRef-Sync | Out-Null

    # This prevents duplicate assembly attribute errors from stale obj/bin folders
    C-Clean
}

function Find-ParentProjectByName {
    # The nearest existing project whose name the given name extends, walking up the hierarchy:
    # Compze.Utilities.Threading.Testing -> Compze.Utilities.Threading, then Compze.Utilities. Returns its name and file.
    param([string]$SolutionPath, [string]$ProjectName)

    $projectParts = $ProjectName -split '\.'
    for ($i = $projectParts.Length - 2; $i -gt 0; $i--) {
        $candidateParentName = ($projectParts[0..$i]) -join '.'
        $candidateParentFile = Find-ProjectFile -SolutionPath $SolutionPath -ProjectName $candidateParentName
        if ($candidateParentFile) {
            return $candidateParentName, $candidateParentFile
        }
    }

    return $null, $null
}

function Test-IsInsideDirectory {
    param([string]$Path, [string]$Directory)

    $directoryWithSeparator = [System.IO.Path]::TrimEndingDirectorySeparator([System.IO.Path]::GetFullPath($Directory)) + [System.IO.Path]::DirectorySeparatorChar
    return [System.IO.Path]::GetFullPath($Path).StartsWith($directoryWithSeparator, [System.StringComparison]::OrdinalIgnoreCase)
}

function Move-InternalsVisibleToGrant {
    # A grant of access to a project's internals that names the merged project's assembly names the target's instead,
    # or goes, when the project already grants the target access.
    param([string]$CsprojPath, [string]$From, [string]$To)

    [xml]$xml = Get-Content $CsprojPath
    $grants = @($xml.SelectNodes("//InternalsVisibleTo[@Include]"))
    $grantToTheMergedProject = $grants | Where-Object { $_.GetAttribute("Include") -eq $From } | Select-Object -First 1
    if (-not $grantToTheMergedProject) {
        return
    }

    if ($grants | Where-Object { $_.GetAttribute("Include") -eq $To }) {
        $grantToTheMergedProject.ParentNode.RemoveChild($grantToTheMergedProject) | Out-Null
    }
    else {
        $grantToTheMergedProject.SetAttribute("Include", $To)
    }

    Save-XmlWithThreeSpacesIndentation -Xml $xml -Path $CsprojPath
}

function Copy-ReferencesToTheProjectItIsMergedInto {
    # The merged project's code needs what it referenced: its project references, other than to the target, which
    # C-FlexRef-Sync then pairs; its package references outside the FlexRef pairs, such as MemoryPack; and its grants
    # of access to its internals, which its code moves into the target with.
    param([string]$CsprojPath, [string]$TargetCsprojPath, [string]$TargetProjectName)

    $mergedProjectDir = Split-Path -Parent $CsprojPath
    $targetProjectDir = Split-Path -Parent $TargetCsprojPath
    $targetProjectReferences = @(Get-ProjectReferences -CsprojPath $TargetCsprojPath)

    foreach ($reference in Get-ProjectReferences -CsprojPath $CsprojPath) {
        if ($reference -like "*$TargetProjectName.csproj") {
            continue
        }

        $referencedProjectName = [System.IO.Path]::GetFileName($reference)
        if ($targetProjectReferences | Where-Object { [System.IO.Path]::GetFileName($_) -eq $referencedProjectName }) {
            continue
        }

        $referencedProjectPath = [System.IO.Path]::GetFullPath((Join-Path $mergedProjectDir $reference))
        Add-ProjectReference -CsprojPath $TargetCsprojPath -ReferencePath ([System.IO.Path]::GetRelativePath($targetProjectDir, $referencedProjectPath))
    }

    [xml]$mergedXml = Get-Content $CsprojPath
    [xml]$targetXml = Get-Content $TargetCsprojPath
    $targetChanged = $false

    $plainPackageReferences = @($mergedXml.SelectNodes("//ItemGroup[not(@Condition)]/PackageReference[@Include]"))
    foreach ($packageReference in $plainPackageReferences) {
        $packageId = $packageReference.GetAttribute("Include")
        if ($targetXml.SelectSingleNode("//PackageReference[@Include='$packageId']")) {
            continue
        }
        Add-ToAnUnconditionalItemGroupHolding -Xml $targetXml -ElementName "PackageReference" -Element $targetXml.ImportNode($packageReference, $true)
        $targetChanged = $true
    }

    foreach ($grant in @($mergedXml.SelectNodes("//InternalsVisibleTo[@Include]"))) {
        $grantee = $grant.GetAttribute("Include")
        if ($grantee -eq $TargetProjectName -or $targetXml.SelectSingleNode("//InternalsVisibleTo[@Include='$grantee']")) {
            continue
        }
        Add-ToAnUnconditionalItemGroupHolding -Xml $targetXml -ElementName "InternalsVisibleTo" -Element $targetXml.ImportNode($grant, $true)
        $targetChanged = $true
    }

    if ($targetChanged) {
        Save-XmlWithThreeSpacesIndentation -Xml $targetXml -Path $TargetCsprojPath
    }
}

function Add-ToAnUnconditionalItemGroupHolding {
    param([xml]$Xml, [string]$ElementName, [System.Xml.XmlNode]$Element)

    $itemGroup = $Xml.SelectSingleNode("//ItemGroup[not(@Condition) and $ElementName]")
    if (-not $itemGroup) {
        $itemGroup = $Xml.CreateElement("ItemGroup")
        $Xml.DocumentElement.AppendChild($itemGroup) | Out-Null
    }
    $itemGroup.AppendChild($Element) | Out-Null
}
