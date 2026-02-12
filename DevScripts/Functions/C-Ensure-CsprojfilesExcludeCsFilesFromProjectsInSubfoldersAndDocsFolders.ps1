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
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    $srcPath = Join-Path $script:CompzeRoot "src"

    function Get-RelativeSubdirectory {
        param(
            [string]$parentPath,
            [string]$childPath
        )
        
        $relativePath = $childPath.Substring($parentPath.Length).TrimStart('\', '/')
        $firstSegment = $relativePath.Split('\')[0]
        return $firstSegment
    }

    function Remove-ExistingExclusions {
        param(
            [string]$content
        )
        
        # Remove the exclusion comment(s) for subdirectories
        $content = $content -replace '(?s)\s*<!--[^>]*Exclude subdirectories[^>]*?-->', ''
        
        # Remove the _docs comment
        $content = $content -replace '(?s)\s*<!--[^>]*Exclude _docs files[^>]*?-->', ''
        
        # Remove all existing ItemGroups that contain only Remove entries
        $content = $content -replace '(?s)\s*<ItemGroup>\s*(?:<(?:Compile|EmbeddedResource|None|Content) (?:Remove|Include)="[^"]+"\s*/>\s*)+</ItemGroup>', ''
        
        # Also remove standalone Remove/Include entries that might be outside ItemGroups
        $content = $content -replace '\s*<(?:Compile|EmbeddedResource|None|Content) (?:Remove|Include)="[^"]+"\s*/>', ''
        
        return $content
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
            $docsLines += "    <Content Include=`"$folder\**\*.md`" />"
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

    $processedCount = 0

    foreach ($csprojFile in $allCsprojFiles) {
        $projectDir = $csprojFile.Directory.FullName
        
        # Find all .csproj files in subdirectories (not siblings)
        $childProjects = Get-ProjectFilesInPath -Path $projectDir | 
            Where-Object { 
                $_.FullName -ne $csprojFile.FullName -and
                $_.Directory.FullName -ne $projectDir
            }
        
        # Find all _docs directories in this project
        $docsFolders = Get-ChildItem -Path $projectDir -Directory -Filter "_docs" -Recurse | 
            ForEach-Object {
                $relativePath = $_.FullName.Substring($projectDir.Length).TrimStart('\', '/')
                $relativePath
            } | Sort-Object
        
        $content = Get-Content $csprojFile.FullName -Raw
        
        # Check if this file has our auto-generated sections
        $hasAutoGeneratedSections = ($content -match 'This section is automatically maintained by DevScripts\\C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders\.ps1')
        
        # Skip if: no child projects AND no docs folders AND no auto-generated sections to clean up
        if ($childProjects.Count -eq 0 -and $docsFolders.Count -eq 0 -and -not $hasAutoGeneratedSections) {
            continue
        }
        
        # ALWAYS remove existing auto-generated sections first
        $content = Remove-ExistingExclusions -content $content
        
        # Build the new ItemGroups based on what currently exists
        $itemGroups = @()
        
        # Add _docs ItemGroup if there are docs folders
        if ($docsFolders.Count -gt 0) {
            $docsItemGroup = Build-DocsItemGroup -docsFolders $docsFolders
            $itemGroups += $docsItemGroup
        }
        
        # Add subfolder exclusions if there are child projects
        if ($childProjects.Count -gt 0) {
            $subdirsToExclude = $childProjects | ForEach-Object {
                $childProjDir = $_.Directory.FullName
                $relativePath = $childProjDir.Substring($projectDir.Length).TrimStart('\', '/')
                $relativePath
            } | Select-Object -Unique | Sort-Object
            
            $patterns = $subdirsToExclude | ForEach-Object { "$_\**" }
            $exclusionItemGroup = Build-ExclusionItemGroup -patterns $patterns
            $itemGroups += $exclusionItemGroup
        }
        
        # Combine all item groups (could be empty if all children were deleted)
        $allItemGroups = $itemGroups -join "`r`n"
        
        # Remove any extra blank lines before </Project> and add the new ItemGroups (if any)
        if ($allItemGroups) {
            $content = $content -replace '\s*</Project>', "`r`n$allItemGroups`r`n</Project>"
        } else {
            # Just clean up whitespace if we removed sections but have nothing to add
            $content = $content -replace '\s*</Project>', "`r`n</Project>"
        }
        
        Set-Content -Path $csprojFile.FullName -Value $content -NoNewline
        
        $processedCount++
    }
}
