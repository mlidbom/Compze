# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders {
    <#
    .SYNOPSIS
    Ensures .csproj files exclude .cs files from projects in subfolders and properly handle _docs folders

    .DESCRIPTION
    For each project under src/ and test/, maintains the two sections of its .csproj that this command writes, each a
    comment saying so followed by an ItemGroup:
    - a project with other projects in its subfolders removes their files from its own (Compile, EmbeddedResource,
      None, Content);
    - a project with _docs folders compiles none of their .cs files, while keeping them visible in Solution Explorer.

    A section is rewritten where it stands, added before </Project> when it is new, and removed when nothing calls for
    it any more. Nothing else in a .csproj is touched, and a file whose sections are already right is not written.

    Which projects and _docs folders there are is what git sees: the files it tracks, and the new ones it doesn't
    ignore. Build output, such as the website's _site, and folders that link to others, such as its Compze junctions,
    are ignored, so their copies of _docs folders and projects count for nothing.
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $maintainedBy = 'This section is automatically maintained by DevScripts\C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders.ps1'
    $docsSectionPattern = '(?s)\r?\n[ \t]*<!-- Exclude _docs files from compilation but keep them visible\.[^>]*?-->\s*<ItemGroup>.*?</ItemGroup>'
    $subdirectoriesSectionPattern = '(?s)\r?\n[ \t]*<!-- Exclude subdirectories that have their own \.csproj files[^>]*?-->\s*<ItemGroup>.*?</ItemGroup>'

    function Get-SourceFiles {
        # The repository-relative paths, with forward slashes, of the files under src/ and test/ that git tracks and
        # still exist, and of the new ones it doesn't ignore.
        $listed = git -C $script:CompzeRoot ls-files --cached --others --exclude-standard -- src test
        if ($LASTEXITCODE -ne 0) { throw "git ls-files failed in $($script:CompzeRoot)" }
        $deleted = [System.Collections.Generic.HashSet[string]]::new([string[]]@(git -C $script:CompzeRoot ls-files --deleted -- src test))
        return @($listed | Where-Object { -not $deleted.Contains($_) } | Select-Object -Unique)
    }

    function Build-DocsSection {
        param([string[]]$docsFolders)

        $lines = foreach ($folder in $docsFolders | Sort-Object) {
            "    <Compile Remove=`"$folder\**\*.cs`" />"
            "    <None Include=`"$folder\**\*.cs`" />"
        }

        return @"
  <!-- Exclude _docs files from compilation but keep them visible.
       $maintainedBy -->
  <ItemGroup>
$($lines -join "`r`n")
  </ItemGroup>
"@
    }

    function Build-SubdirectoriesSection {
        param([string[]]$subdirectories)

        $lines = foreach ($subdirectory in $subdirectories | Sort-Object) {
            foreach ($itemType in 'Compile', 'EmbeddedResource', 'None', 'Content') {
                "    <$itemType Remove=`"$subdirectory\**`" />"
            }
        }

        return @"
  <!-- Exclude subdirectories that have their own .csproj files to prevent compilation errors.
       $maintainedBy -->
  <ItemGroup>
$($lines -join "`r`n")
  </ItemGroup>
"@
    }

    function Set-MaintainedSection {
        # The content with the section that the pattern finds replaced by the new one where it stands, the new one added
        # before </Project> if there was none, or the section removed if the new one is empty. Duplicates are removed.
        param([string]$content, [string]$sectionPattern, [string]$section)

        $found = [regex]::Matches($content, $sectionPattern)
        if ($found.Count -eq 0) {
            if (-not $section) { return $content }
            return $content -replace '\s*</Project>\s*$', "`r`n$section`r`n</Project>"
        }

        for ($index = $found.Count - 1; $index -ge 0; $index--) {
            $match = $found[$index]
            $replacement = if ($index -eq 0 -and $section) { "`r`n$section" } else { '' }
            $content = $content.Remove($match.Index, $match.Length).Insert($match.Index, $replacement)
        }
        return $content
    }

    $sourceFiles = Get-SourceFiles
    $projectFiles = @($sourceFiles | Where-Object { $_ -like '*.csproj' })

    foreach ($projectFile in $projectFiles) {
        $projectDirectory = Split-Path $projectFile -Parent
        $prefix = "$($projectDirectory -replace '\\', '/')/"

        $childProjectDirectories = @($projectFiles |
            Where-Object { $_.StartsWith($prefix) -and $_ -ne $projectFile } |
            ForEach-Object { (Split-Path $_.Substring($prefix.Length) -Parent) -replace '/', '\' } |
            Where-Object { $_ } |
            Select-Object -Unique)

        $docsFolders = @($sourceFiles |
            Where-Object { $_.StartsWith($prefix) } |
            ForEach-Object { $_.Substring($prefix.Length) -replace '/', '\' } |
            Where-Object { $file = $_; -not ($childProjectDirectories | Where-Object { $file.StartsWith("$_\") }) } |
            ForEach-Object { if ($_ -match '^(.*?(?:^|\\)_docs)\\') { $Matches[1] } } |
            Select-Object -Unique)

        $docsSection = if ($docsFolders.Count -gt 0) { Build-DocsSection -docsFolders $docsFolders } else { '' }
        $subdirectoriesSection = if ($childProjectDirectories.Count -gt 0) { Build-SubdirectoriesSection -subdirectories $childProjectDirectories } else { '' }

        $projectPath = Join-Path $script:CompzeRoot $projectFile
        $content = Get-Content $projectPath -Raw
        $updated = Set-MaintainedSection -content $content -sectionPattern $docsSectionPattern -section $docsSection
        $updated = Set-MaintainedSection -content $updated -sectionPattern $subdirectoriesSectionPattern -section $subdirectoriesSection

        if ($updated -cne $content) {
            Set-Content -Path $projectPath -Value $updated -NoNewline
        }
    }
}
