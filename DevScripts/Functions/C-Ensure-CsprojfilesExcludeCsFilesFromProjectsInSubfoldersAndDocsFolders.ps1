# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders {
    <#
    .SYNOPSIS
    Ensures .csproj files exclude .cs files from projects in subfolders and properly handle _docs folders

    .DESCRIPTION
    Finds all .csproj files that have other .csproj files in subdirectories and ensures complete exclusions
    are in place (Compile, EmbeddedResource, None, Content). Also detects _docs folders and ensures they are
    properly configured to exclude .cs files from compilation while keeping them visible in Solution Explorer.
    All exclusions are consolidated into well-documented ItemGroups.

    It rewrites only the ItemGroups it generates, which carry its marker comment, and leaves every other ItemGroup of a
    project file alone, hand-written exclusions included. The rewritten sections stay where they were in the file; a
    project file that has none gets them before </Project>. It looks for _docs folders in the project's own sources only:
    never in build output (bin, obj, a DocFX site's _site), in node_modules, in a nested project's folder, or through a
    junction or symbolic link.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $srcPath = Join-Path $script:CompzeRoot "src"
    $foldersThatHoldNoSources = @('bin', 'obj', '_site', 'node_modules')
    $subdirectoriesSectionPattern = '(?s)[ \t]*<!-- Exclude subdirectories that have their own \.csproj files[^>]*?automatically maintained by DevScripts[^>]*?-->\s*<ItemGroup>.*?</ItemGroup>[ \t]*(\r?\n)?'
    $docsSectionPattern = '(?s)[ \t]*<!-- Exclude _docs files from compilation[^>]*?automatically maintained by DevScripts[^>]*?-->\s*<ItemGroup>.*?</ItemGroup>[ \t]*(\r?\n)?'

    function Get-DocsFoldersOfProject {
        param(
            [string]$projectDir
        )

        $docsFolders = @()
        $foldersToSearch = [System.Collections.Generic.Queue[System.IO.DirectoryInfo]]::new()
        $foldersToSearch.Enqueue([System.IO.DirectoryInfo]::new($projectDir))
        while ($foldersToSearch.Count -gt 0) {
            $folder = $foldersToSearch.Dequeue()
            foreach ($subfolder in $folder.GetDirectories()) {
                $isLinkOrJunction = $subfolder.Attributes.HasFlag([System.IO.FileAttributes]::ReparsePoint)
                $holdsNoSources = $foldersThatHoldNoSources -contains $subfolder.Name
                $isNestedProject = @($subfolder.GetFiles('*.csproj')).Count -gt 0
                if ($isLinkOrJunction -or $holdsNoSources -or $isNestedProject) { continue }

                if ($subfolder.Name -eq '_docs') {
                    $docsFolders += $subfolder.FullName.Substring($projectDir.Length).TrimStart('\', '/')
                } else {
                    $foldersToSearch.Enqueue($subfolder)
                }
            }
        }
        return $docsFolders | Sort-Object
    }

    function Build-ExclusionItemGroup {
        param(
            [string[]]$patterns
        )

        $exclusionLines = @()
        foreach ($pattern in $patterns | Sort-Object) {
            $exclusionLines += "    <Compile Remove=`"$pattern`" />"
            $exclusionLines += "    <EmbeddedResource Remove=`"$pattern`" />"
            $exclusionLines += "    <None Remove=`"$pattern`" />"
            $exclusionLines += "    <Content Remove=`"$pattern`" />"
        }

        $comment = @"
  <!-- Exclude subdirectories that have their own .csproj files to prevent compilation errors.
       This section is automatically maintained by DevScripts\C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders.ps1 -->
"@

        $itemGroup = @"
$comment
  <ItemGroup>
$($exclusionLines -join "`r`n")
  </ItemGroup>
"@

        return $itemGroup
    }

    function Build-DocsItemGroup {
        param(
            [string[]]$docsFolders
        )

        if ($docsFolders.Count -eq 0) {
            return ""
        }

        $docsLines = @()
        foreach ($folder in $docsFolders | Sort-Object) {
            $docsLines += "    <Compile Remove=`"$folder\**\*.cs`" />"
            $docsLines += "    <None Include=`"$folder\**\*.cs`" />"
        }

        $comment = @"
  <!-- Exclude _docs files from compilation but keep them visible.
       This section is automatically maintained by DevScripts\C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders.ps1 -->
"@

        $itemGroup = @"
$comment
  <ItemGroup>
$($docsLines -join "`r`n")
  </ItemGroup>
"@

        return $itemGroup
    }

    # Find all .csproj files in src/ and test/
    $allCsprojFiles = @()
    $allCsprojFiles += Get-ProjectFilesInPath -Path $srcPath
    $testPath = Join-Path $script:CompzeRoot "test"
    if (Test-Path $testPath) {
        $allCsprojFiles += Get-ProjectFilesInPath -Path $testPath
    }

    foreach ($csprojFile in $allCsprojFiles) {
        $projectDir = $csprojFile.Directory.FullName

        # Find all .csproj files in subdirectories (not siblings)
        $childProjects = Get-ProjectFilesInPath -Path $projectDir |
            Where-Object {
                $_.FullName -ne $csprojFile.FullName -and
                $_.Directory.FullName -ne $projectDir
            }

        $docsFolders = @(Get-DocsFoldersOfProject -projectDir $projectDir)

        $originalContent = Get-Content $csprojFile.FullName -Raw

        $generatedSections = @([regex]::Matches($originalContent, $docsSectionPattern)) + @([regex]::Matches($originalContent, $subdirectoriesSectionPattern))

        # Skip if: no child projects AND no docs folders AND no generated sections to clean up
        if ($childProjects.Count -eq 0 -and $docsFolders.Count -eq 0 -and $generatedSections.Count -eq 0) {
            continue
        }

        # Build the new ItemGroups based on what currently exists
        $itemGroups = @()

        if ($docsFolders.Count -gt 0) {
            $itemGroups += Build-DocsItemGroup -docsFolders $docsFolders
        }

        if ($childProjects.Count -gt 0) {
            $subdirsToExclude = $childProjects | ForEach-Object {
                $childProjDir = $_.Directory.FullName
                $relativePath = $childProjDir.Substring($projectDir.Length).TrimStart('\', '/')
                $relativePath
            } | Select-Object -Unique | Sort-Object

            $patterns = $subdirsToExclude | ForEach-Object { "$_\**" }
            $itemGroups += Build-ExclusionItemGroup -patterns $patterns
        }

        $newSections = if ($itemGroups.Count -gt 0) { ($itemGroups -join "`r`n") + "`r`n" } else { "" }

        if ($generatedSections.Count -gt 0) {
            # The new sections take the place of the first generated section; the others are removed.
            $firstSectionIndex = ($generatedSections | Measure-Object -Property Index -Minimum).Minimum
            $content = $originalContent
            foreach ($section in $generatedSections | Sort-Object Index -Descending) {
                $content = $content.Remove($section.Index, $section.Length)
            }
            $content = $content.Insert($firstSectionIndex, $newSections)
        } else {
            $content = $originalContent -replace '\s*</Project>', "`r`n$($newSections.TrimEnd())`r`n</Project>"
        }

        if ($content -ne $originalContent) {
            Set-Content -Path $csprojFile.FullName -Value $content -NoNewline
        }
    }
}
