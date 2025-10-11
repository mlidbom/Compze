function Ensure-CompzeCsprojfilesExcludeCsFilesFromProjectsInSubfolders {
    <#
    .SYNOPSIS
    Ensures .csproj files exclude .cs files from projects in subfolders
    
    .DESCRIPTION
    Finds all .csproj files that have other .csproj files in subdirectories
    and ensures complete exclusions are in place (Compile, EmbeddedResource, None, Content).
    All exclusions are consolidated into a single, well-documented ItemGroup at the end of the file.
    This ensures subdirectories are completely hidden from Visual Studio Solution Explorer.
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
        
        # Remove the exclusion comment(s) - handles both single and multi-line comments
        $content = $content -replace '(?s)\s*<!--[^>]*Exclude subdirectories[^>]*?-->', ''
        
        # Remove all existing ItemGroups that contain only Remove entries
        # This handles both compact and spread-out ItemGroups
        $content = $content -replace '(?s)\s*<ItemGroup>\s*(?:<(?:Compile|EmbeddedResource|None|Content) Remove="[^"]+"\s*/>\s*)+</ItemGroup>', ''
        
        # Also remove standalone Remove entries that might be outside ItemGroups
        $content = $content -replace '\s*<(?:Compile|EmbeddedResource|None|Content) Remove="[^"]+"\s*/>', ''
        
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
       This section is automatically maintained by DevScripts\Ensure-CompzeCsprojfilesExcludeCsFilesFromProjectsInSubfolders.ps1 -->
"@
        
        $itemGroup = @"
$comment
  <ItemGroup>
$($exclusionLines -join "`r`n")
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
        
        if ($childProjects.Count -eq 0) {
            continue
        }
        
        Write-Host "`nProcessing: $($csprojFile.FullName)" -ForegroundColor Yellow
        Write-Host "  Found $($childProjects.Count) child project(s)" -ForegroundColor Cyan
        
        # Get the relative path from project dir to each child project's directory
        # Only exclude the specific directory that contains the .csproj, not parent folders
        $subdirsToExclude = $childProjects | ForEach-Object {
            $childProjDir = $_.Directory.FullName
            $relativePath = $childProjDir.Substring($projectDir.Length).TrimStart('\', '/')
            $relativePath
        } | Select-Object -Unique | Sort-Object
        
        $content = Get-Content $csprojFile.FullName -Raw
        
        # Build the patterns we need
        $patterns = $subdirsToExclude | ForEach-Object { "$_\**" }
        
        # Check if we have the consolidated format with the updated comment
        # Force update if we detect duplicate comments or extra blank lines before </Project>
        $hasConsolidatedFormat = ($content -match 'This section is automatically maintained by DevScripts\\Ensure-CompzeCsprojfilesExcludeCsFilesFromProjectsInSubfolders\.ps1') -and 
                                ($content -notmatch '<!--[^>]*Exclude subdirectories[^>]*-->[\s\r\n]*<!--[^>]*Exclude subdirectories') -and
                                ($content -notmatch '</ItemGroup>\s*\r?\n\s*\r?\n\s*\r?\n\s*</Project>')
        
        # Check if we have all the correct exclusions
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
        
        # Reorganize if: missing exclusions OR doesn't have consolidated format
        if (-not $allExclusionsPresent -or -not $hasConsolidatedFormat) {
            # Remove all existing exclusion entries
            $content = Remove-ExistingExclusions -content $content
            
            # Build the new consolidated ItemGroup
            $exclusionItemGroup = Build-ExclusionItemGroup -patterns $patterns
            
            # Remove any extra blank lines before </Project> and add the new ItemGroup
            $content = $content -replace '\s*</Project>', "`r`n$exclusionItemGroup`r`n</Project>"
            
            Set-Content -Path $csprojFile.FullName -Value $content -NoNewline
            Write-Host "  ✓ Updated: $($csprojFile.Name)" -ForegroundColor Green
            
            foreach ($subdir in $subdirsToExclude) {
                Write-Host "    - Excluded: $subdir\" -ForegroundColor Cyan
            }
            
            $processedCount++
        } else {
            Write-Host "  Already correctly configured: $($csprojFile.Name)" -ForegroundColor Gray
        }
    }

    Write-Host "`n✓ Complete! Updated $processedCount project file(s)." -ForegroundColor Green
}
