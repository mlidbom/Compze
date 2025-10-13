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
  <!-- Exclude subdirectories that have their own .csproj files to hide them from Solution Explorer.
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

    # Find all .csproj files
    $allCsprojFiles = Get-ChildItem -Path $srcPath -Filter "*.csproj" -Recurse
    Write-Host "Found $($allCsprojFiles.Count) total .csproj files" -ForegroundColor Cyan

    $processedCount = 0

    foreach ($csprojFile in $allCsprojFiles) {
        $projectDir = $csprojFile.Directory.FullName
        
        # Find all .csproj files in subdirectories
        $childProjects = Get-ChildItem -Path $projectDir -Filter "*.csproj" -Recurse | 
            Where-Object { $_.FullName -ne $csprojFile.FullName }
        
        # Find all _docs directories in this project
        $docsFolders = Get-ChildItem -Path $projectDir -Directory -Filter "_docs" -Recurse | 
            ForEach-Object {
                $relativePath = $_.FullName.Substring($projectDir.Length).TrimStart('\', '/')
                $relativePath
            } | Sort-Object
        
        if ($childProjects.Count -eq 0 -and $docsFolders.Count -eq 0) {
            continue
        }
        
        Write-Host "`nProcessing: $($csprojFile.FullName)" -ForegroundColor Yellow
        if ($childProjects.Count -gt 0) {
            Write-Host "  Found $($childProjects.Count) child project(s)" -ForegroundColor Cyan
        }
        if ($docsFolders.Count -gt 0) {
            Write-Host "  Found $($docsFolders.Count) _docs folder(s)" -ForegroundColor Cyan
        }
        
        # Get the relative path from project dir to each child project's directory
        $subdirsToExclude = $childProjects | ForEach-Object {
            $childProjDir = $_.Directory.FullName
            $relativePath = $childProjDir.Substring($projectDir.Length).TrimStart('\', '/')
            $relativePath
        } | Select-Object -Unique | Sort-Object
        
        $content = Get-Content $csprojFile.FullName -Raw
        
        # Build the patterns we need
        $patterns = $subdirsToExclude | ForEach-Object { "$_\**" }
        
        # Check if we have the consolidated format with the updated comment
        $hasConsolidatedFormat = ($content -match 'This section is automatically maintained by DevScripts\\C-Ensure-CsprojfilesExcludeCsFilesFromProjectsInSubfoldersAndDocsFolders\.ps1')
        
        # Check if we have all the correct exclusions for subfolders
        $allExclusionsPresent = $true
        foreach ($pattern in $patterns) {
            $hasCompile = $content -match [regex]::Escape("<Compile Remove=`"$pattern`"")
            $hasEmbeddedResource = $content -match [regex]::Escape("<EmbeddedResource Remove=`"$pattern`"")
            $hasNone = $content -match [regex]::Escape("<None Remove=`"$pattern`"")
            $hasContent = $content -match [regex]::Escape("<Content Remove=`"$pattern`"")
            
            if (-not ($hasCompile -and $hasEmbeddedResource -and $hasNone -and $hasContent)) {
                $allExclusionsPresent = $false
                break
            }
        }
        
        # Check if we have all the correct _docs configurations
        $allDocsPresent = $true
        foreach ($folder in $docsFolders) {
            $hasCompileRemove = $content -match [regex]::Escape("<Compile Remove=`"$folder\**\*.cs`"")
            $hasNoneInclude = $content -match [regex]::Escape("<None Include=`"$folder\**\*.cs`"")
            $hasContentInclude = $content -match [regex]::Escape("<Content Include=`"$folder\**\*.md`"")
            
            if (-not ($hasCompileRemove -and $hasNoneInclude -and $hasContentInclude)) {
                $allDocsPresent = $false
                break
            }
        }
        
        # Reorganize if: missing exclusions OR missing docs config OR doesn't have consolidated format
        if (-not $allExclusionsPresent -or -not $allDocsPresent -or -not $hasConsolidatedFormat) {
            # Remove all existing exclusion entries
            $content = Remove-ExistingExclusions -content $content
            
            # Build the new ItemGroups
            $itemGroups = @()
            
            # Add _docs ItemGroup first if there are docs folders
            if ($docsFolders.Count -gt 0) {
                $docsItemGroup = Build-DocsItemGroup -docsFolders $docsFolders
                $itemGroups += $docsItemGroup
            }
            
            # Add subfolder exclusions if there are child projects
            if ($patterns.Count -gt 0) {
                $exclusionItemGroup = Build-ExclusionItemGroup -patterns $patterns
                $itemGroups += $exclusionItemGroup
            }
            
            # Combine all item groups
            $allItemGroups = $itemGroups -join "`r`n"
            
            # Remove any extra blank lines before </Project> and add the new ItemGroups
            $content = $content -replace '\s*</Project>', "`r`n$allItemGroups`r`n</Project>"
            
            Set-Content -Path $csprojFile.FullName -Value $content -NoNewline
            Write-Host "  ✓ Updated: $($csprojFile.Name)" -ForegroundColor Green
            
            if ($docsFolders.Count -gt 0) {
                foreach ($folder in $docsFolders) {
                    Write-Host "    - Configured _docs: $folder\" -ForegroundColor Cyan
                }
            }
            
            if ($subdirsToExclude.Count -gt 0) {
                foreach ($subdir in $subdirsToExclude) {
                    Write-Host "    - Excluded subfolder: $subdir\" -ForegroundColor Cyan
                }
            }
            
            $processedCount++
        } else {
            Write-Host "  Already correctly configured: $($csprojFile.Name)" -ForegroundColor Gray
        }
    }

    Write-Host "`n✓ Complete! Updated $processedCount project file(s)." -ForegroundColor Green
}
