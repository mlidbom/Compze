function C-Relocate-Project {
    <#
    .SYNOPSIS
    Relocates a project to match the solution structure conventions
    
    .DESCRIPTION
    Moves a project in the file system to match the solution structure rules where:
    - Project name Compze.A.B.C should be in directory Compze/A/B/C
    - Solution folder should match the directory structure
    
    The script performs these operations:
    - Finds the current location of the project
    - Calculates the target location based on project name
    - Moves the project directory to the new location
    - Updates all ProjectReference paths in .csproj files
    - Updates Project Path in all solution files
    - Updates solution folder structure in .slnx files
    
    .PARAMETER ProjectName
    The name of the project to relocate (e.g., "Compze.Common.Configuration")
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)
    
    .EXAMPLE
    C-Relocate-Project Compze.Common.Configuration
    Moves Compze.Common.Configuration to src/Compze/Common/Configuration
    
    .EXAMPLE
    C-Relocate-Project Compze.Sql.DocumentDb -WhatIf
    Preview where the project would be moved without actually moving it
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        
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
    
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "PROJECT RELOCATION OPERATION" -ForegroundColor Cyan
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "Project: $ProjectName" -ForegroundColor Yellow
    Write-Host "Solution: $SolutionPath" -ForegroundColor Cyan
    Write-Host ""
    
    # Step 1: Find the project file
    Write-Host "[Step 1/6] Finding project file..." -ForegroundColor Cyan
    
    $projectFileName = "$ProjectName.csproj"
    $projectFile = Get-ChildItem -Path $solutionDir -Filter $projectFileName -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if (-not $projectFile) {
        Write-Error "Project file '$projectFileName' not found in solution directory"
        return
    }
    
    $currentProjectDir = Split-Path -Parent $projectFile.FullName
    $currentRelativePath = $currentProjectDir.Substring($solutionDir.Length + 1)
    
    Write-Host "  Found: $($projectFile.FullName)" -ForegroundColor Green
    Write-Host "  Current location: $currentRelativePath" -ForegroundColor Yellow
    Write-Host ""
    
    # Step 2: Calculate target location based on project name
    Write-Host "[Step 2/6] Calculating target location..." -ForegroundColor Cyan
    
    # Convert project name to path: Compze.A.B.C -> Compze/A/B/C
    $targetRelativePath = $ProjectName -replace '\.', '\'
    $targetProjectDir = Join-Path $solutionDir $targetRelativePath
    
    # For solution file, use forward slashes
    $targetSolutionPath = $ProjectName -replace '\.', '/'
    $targetSolutionFolder = "/" + ($ProjectName -replace '\.', '/')
    
    # Remove the last segment (project name) to get the parent folder
    $targetSolutionFolder = $targetSolutionFolder.Substring(0, $targetSolutionFolder.LastIndexOf('/'))
    
    Write-Host "  Target directory: $targetRelativePath" -ForegroundColor Green
    Write-Host "  Target solution folder: $targetSolutionFolder" -ForegroundColor Green
    Write-Host ""
    
    # Check if already in correct location
    if ($currentProjectDir -eq $targetProjectDir) {
        Write-Host "Project is already in the correct location!" -ForegroundColor Green
        return
    }
    
    # Step 3: Move the project directory
    Write-Host "[Step 3/6] Moving project directory..." -ForegroundColor Cyan
    
    if (Test-Path $targetProjectDir) {
        Write-Error "Target directory already exists: $targetProjectDir"
        return
    }
    
    # Create parent directory if it doesn't exist
    $targetParentDir = Split-Path -Parent $targetProjectDir
    if (-not (Test-Path $targetParentDir)) {
        if ($PSCmdlet.ShouldProcess($targetParentDir, "Create directory")) {
            New-Item -ItemType Directory -Path $targetParentDir -Force | Out-Null
            Write-Host "  Created parent directory: $targetParentDir" -ForegroundColor Green
        }
    }
    
    if ($PSCmdlet.ShouldProcess($currentProjectDir, "Move to $targetProjectDir")) {
        Move-Item -Path $currentProjectDir -Destination $targetProjectDir
        Write-Host "  ✓ Moved: $currentRelativePath -> $targetRelativePath" -ForegroundColor Green
    }
    Write-Host ""
    
    # Step 4: Update ProjectReference elements in all .csproj files
    Write-Host "[Step 4/6] Updating ProjectReference elements in .csproj files..." -ForegroundColor Cyan
    
    $allCsprojFiles = Get-ChildItem -Path $solutionDir -Filter "*.csproj" -Recurse
    $projectReferencesUpdated = 0
    
    # Old path pattern (could be relative from various locations)
    $oldPathPattern = [regex]::Escape($ProjectName + ".csproj")
    
    foreach ($csproj in $allCsprojFiles) {
        $content = Get-Content $csproj.FullName -Raw
        $modified = $false
        
        # Find and replace all ProjectReference elements that reference our project
        $pattern = '<ProjectReference\s+Include="([^"]*' + $oldPathPattern + ')"'
        
        # Use -replace callback to calculate new paths
        $newContent = [regex]::Replace($content, $pattern, {
            param($m)
            $oldPath = $m.Groups[1].Value
            
            # Calculate new relative path from this csproj to the target location
            $csprojDir = Split-Path -Parent $csproj.FullName
            $newPath = [System.IO.Path]::GetRelativePath($csprojDir, (Join-Path $targetProjectDir $projectFileName))
            
            # Replace backslashes with forward slashes for consistency
            $newPath = $newPath -replace '\\', '/'
            
            # Only mark as modified if path actually changed
            if ($oldPath -ne $newPath) {
                $script:modified = $true
            }
            
            return '<ProjectReference Include="' + $newPath + '"'
        })
        
        if ($modified) {
            $content = $newContent
            
            if ($PSCmdlet.ShouldProcess($csproj.FullName, "Update ProjectReference paths")) {
                Set-Content -Path $csproj.FullName -Value $content -NoNewline -Encoding UTF8
                $projectReferencesUpdated++
                Write-Host "  ✓ Updated: $($csproj.Name)" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "  Updated $projectReferencesUpdated ProjectReference(s)" -ForegroundColor Green
    Write-Host ""
    
    # Step 5: Update solution files - Project Path
    Write-Host "[Step 5/6] Updating Project Path in solution files..." -ForegroundColor Cyan
    
    $slnxFiles = Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse
    $slnFiles = Get-ChildItem -Path $solutionDir -Filter "*.sln" -Recurse
    $allSolutionFiles = @($slnxFiles) + @($slnFiles)
    $solutionPathsUpdated = 0
    
    if ($allSolutionFiles.Count -eq 0) {
        Write-Host "  No solution files found" -ForegroundColor Yellow
    } else {
        foreach ($solutionFile in $allSolutionFiles) {
            $content = Get-Content $solutionFile.FullName -Raw
            
            # Match Project Path with the old project location
            # Pattern: <Project Path="...anything.../ProjectName.csproj" />
            $pattern = '<Project\s+Path="[^"]*' + [regex]::Escape($ProjectName) + '\.csproj"'
            
            if ($content -match $pattern) {
                # Replace with new path (using forward slashes for solution files)
                $newProjectPath = $targetSolutionPath + ".csproj"
                $replacement = '<Project Path="' + $newProjectPath + '"'
                $content = $content -replace $pattern, $replacement
                
                if ($PSCmdlet.ShouldProcess($solutionFile.FullName, "Update Project Path")) {
                    Set-Content -Path $solutionFile.FullName -Value $content -NoNewline -Encoding UTF8
                    $solutionPathsUpdated++
                    Write-Host "  ✓ Updated path in: $($solutionFile.Name)" -ForegroundColor Green
                }
            }
        }
    }
    
    Write-Host "  Updated $solutionPathsUpdated solution file(s)" -ForegroundColor Green
    Write-Host ""
    
    # Step 6: Update solution files - Folder structure (only for .slnx files)
    Write-Host "[Step 6/6] Updating solution folder structure..." -ForegroundColor Cyan
    
    $slnxFoldersUpdated = 0
    
    foreach ($slnxFile in $slnxFiles) {
        [xml]$slnxContent = Get-Content $slnxFile.FullName
        $modified = $false
        
        # Find the project element
        $projectElements = $slnxContent.SelectNodes("//Project[@Path]")
        
        foreach ($projectElement in $projectElements) {
            $path = $projectElement.GetAttribute("Path")
            
            if ($path -like "*$ProjectName.csproj") {
                # Find the parent folder element
                $currentFolderElement = $projectElement.ParentNode
                
                if ($currentFolderElement.Name -eq "Folder") {
                    $currentFolderName = $currentFolderElement.GetAttribute("Name")
                    
                    # Check if we need to move to a different folder
                    if ($currentFolderName -ne $targetSolutionFolder) {
                        Write-Host "  Current folder: $currentFolderName" -ForegroundColor Yellow
                        Write-Host "  Target folder: $targetSolutionFolder" -ForegroundColor Green
                        
                        # Remove project from current folder
                        $currentFolderElement.RemoveChild($projectElement) | Out-Null
                        
                        # Find or create target folder
                        $targetFolderElement = $slnxContent.SelectSingleNode("//Folder[@Name='$targetSolutionFolder']")
                        
                        if (-not $targetFolderElement) {
                            # Create new folder element
                            $targetFolderElement = $slnxContent.CreateElement("Folder")
                            $targetFolderElement.SetAttribute("Name", $targetSolutionFolder)
                            $slnxContent.DocumentElement.AppendChild($targetFolderElement) | Out-Null
                            Write-Host "  Created new solution folder: $targetSolutionFolder" -ForegroundColor Green
                        }
                        
                        # Add project to target folder
                        $targetFolderElement.AppendChild($projectElement) | Out-Null
                        
                        $modified = $true
                    }
                }
            }
        }
        
        if ($modified) {
            if ($PSCmdlet.ShouldProcess($slnxFile.FullName, "Update solution folder structure")) {
                $slnxContent.Save($slnxFile.FullName)
                $slnxFoldersUpdated++
                Write-Host "  ✓ Updated folder structure in: $($slnxFile.Name)" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "  Updated folder structure in $slnxFoldersUpdated solution file(s)" -ForegroundColor Green
    Write-Host ""
    
    # Summary
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "RELOCATION OPERATION COMPLETED" -ForegroundColor Green
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "Project directory moved: Yes" -ForegroundColor Green
    Write-Host "ProjectReferences updated: $projectReferencesUpdated" -ForegroundColor Green
    Write-Host "Solution paths updated: $solutionPathsUpdated" -ForegroundColor Green
    Write-Host "Solution folders updated: $slnxFoldersUpdated" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Build the solution to verify all references are correct" -ForegroundColor White
    Write-Host "  2. Run tests to ensure everything still works" -ForegroundColor White
    Write-Host "  3. Consider running C-Validate-SolutionStructure" -ForegroundColor White
    Write-Host ""
}
