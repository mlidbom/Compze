function C-Rename-Project {
    <#
    .SYNOPSIS
    Renames a project and updates all references including InternalsVisibleTo
    
    .DESCRIPTION
    Renames a project file and updates all references throughout the solution:
    - Renames the project file (.csproj)
    - Updates ProjectReference elements in all .csproj files
    - Updates InternalsVisibleTo attributes in all .csproj files
    - Updates Project Path references in all solution files (.slnx and .sln)
    
    This tool is essential because standard refactoring tools cannot handle
    InternalsVisibleTo attributes, which are extensively used in this codebase.
    
    The script will search for and update ALL solution files found in the solution
    directory, not just a single file.
    
    .PARAMETER Old
    The current name of the project to rename (e.g., "Compze.Tessaging.Hosting.Configuration")
    
    .PARAMETER New
    The new name for the project (e.g., "Compze.Common.Configuration")
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.slnx)
    
    .EXAMPLE
    C-Rename-Project -Old Compze.Tessaging.Hosting.Configuration -New Compze.Common.Configuration
    Renames the project and all references to it
    
    .EXAMPLE
    C-Rename-Project -Old Compze.Old.Name -New Compze.New.Name -SolutionPath "src\MySolution.slnx"
    Renames the project using a custom solution path
    #>
    [CmdletBinding(SupportsShouldProcess)]
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
    
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "PROJECT RENAME OPERATION" -ForegroundColor Cyan
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "Old Name: $Old" -ForegroundColor Yellow
    Write-Host "New Name: $New" -ForegroundColor Green
    Write-Host "Solution: $SolutionPath" -ForegroundColor Cyan
    Write-Host ""
    
    # Step 1: Find the project file
    Write-Host "[Step 1/5] Finding project file..." -ForegroundColor Cyan
    
    $oldProjectFileName = "$Old.csproj"
    $newProjectFileName = "$New.csproj"
    
    # Search for the project file
    $projectFile = Get-ChildItem -Path $solutionDir -Filter $oldProjectFileName -Recurse -ErrorAction SilentlyContinue | Select-Object -First 1
    
    if (-not $projectFile) {
        Write-Error "Project file '$oldProjectFileName' not found in solution directory"
        return
    }
    
    $projectDir = Split-Path -Parent $projectFile.FullName
    $newProjectPath = Join-Path $projectDir $newProjectFileName
    
    Write-Host "  Found: $($projectFile.FullName)" -ForegroundColor Green
    Write-Host "  Will rename to: $newProjectPath" -ForegroundColor Green
    Write-Host ""
    
    # Step 2: Rename the project file
    Write-Host "[Step 2/5] Renaming project file..." -ForegroundColor Cyan
    
    if (Test-Path $newProjectPath) {
        Write-Error "Target project file already exists: $newProjectPath"
        return
    }
    
    if ($PSCmdlet.ShouldProcess($projectFile.FullName, "Rename to $newProjectFileName")) {
        Rename-Item -Path $projectFile.FullName -NewName $newProjectFileName
        Write-Host "  ✓ Renamed: $oldProjectFileName -> $newProjectFileName" -ForegroundColor Green
    }
    Write-Host ""
    
    # Step 3: Update ProjectReference elements in all .csproj files
    Write-Host "[Step 3/5] Updating ProjectReference elements in .csproj files..." -ForegroundColor Cyan
    
    $allCsprojFiles = Get-ChildItem -Path $solutionDir -Filter "*.csproj" -Recurse
    $projectReferencesUpdated = 0
    
    foreach ($csproj in $allCsprojFiles) {
        $content = Get-Content $csproj.FullName -Raw
        
        # Match ProjectReference with the old project name
        # Pattern: <ProjectReference Include="...path...\OldName.csproj" />
        $pattern = '(<ProjectReference\s+Include="[^"]*\\)(' + [regex]::Escape($Old) + ')(\.csproj")'
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, ('$1' + $New + '$3')
            
            if ($PSCmdlet.ShouldProcess($csproj.FullName, "Update ProjectReference")) {
                Set-Content -Path $csproj.FullName -Value $content -NoNewline -Encoding UTF8
                $projectReferencesUpdated++
                Write-Host "  ✓ Updated: $($csproj.Name)" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "  Updated $projectReferencesUpdated ProjectReference(s)" -ForegroundColor Green
    Write-Host ""
    
    # Step 4: Update InternalsVisibleTo attributes in all .csproj files
    Write-Host "[Step 4/5] Updating InternalsVisibleTo attributes in .csproj files..." -ForegroundColor Cyan
    
    $internalsVisibleToUpdated = 0
    
    foreach ($csproj in $allCsprojFiles) {
        $content = Get-Content $csproj.FullName -Raw
        
        # Match InternalsVisibleTo with exact project name
        # Pattern: <InternalsVisibleTo Include="OldName" />
        $pattern = '(<InternalsVisibleTo\s+Include=")(' + [regex]::Escape($Old) + ')(")'
        
        if ($content -match $pattern) {
            $content = $content -replace $pattern, ('$1' + $New + '$3')
            
            if ($PSCmdlet.ShouldProcess($csproj.FullName, "Update InternalsVisibleTo")) {
                Set-Content -Path $csproj.FullName -Value $content -NoNewline -Encoding UTF8
                $internalsVisibleToUpdated++
                Write-Host "  ✓ Updated: $($csproj.Name)" -ForegroundColor Green
            }
        }
    }
    
    Write-Host "  Updated $internalsVisibleToUpdated InternalsVisibleTo attribute(s)" -ForegroundColor Green
    Write-Host ""
    
    # Step 5: Update solution files (.slnx and .sln)
    Write-Host "[Step 5/5] Updating solution files..." -ForegroundColor Cyan
    
    $slnxFiles = Get-ChildItem -Path $solutionDir -Filter "*.slnx" -Recurse
    $slnFiles = Get-ChildItem -Path $solutionDir -Filter "*.sln" -Recurse
    $allSolutionFiles = @($slnxFiles) + @($slnFiles)
    $solutionFilesUpdated = 0
    
    if ($allSolutionFiles.Count -eq 0) {
        Write-Host "  No solution files found" -ForegroundColor Yellow
    } else {
        Write-Host "  Found $($allSolutionFiles.Count) solution file(s) to check" -ForegroundColor Cyan
        
        foreach ($solutionFile in $allSolutionFiles) {
            $content = Get-Content $solutionFile.FullName -Raw
            
            # Match Project Path with the old project name
            # Pattern: <Project Path="...path.../OldName.csproj" /> (for .slnx files)
            # Note: Solution files use forward slashes, so we match both / and \
            $pattern = '(<Project\s+Path="[^"]*[/\\])(' + [regex]::Escape($Old) + ')(\.csproj")'
            
            if ($content -match $pattern) {
                $content = $content -replace $pattern, ('$1' + $New + '$3')
                
                if ($PSCmdlet.ShouldProcess($solutionFile.FullName, "Update Project Path")) {
                    Set-Content -Path $solutionFile.FullName -Value $content -NoNewline -Encoding UTF8
                    $solutionFilesUpdated++
                    Write-Host "  ✓ Updated: $($solutionFile.Name)" -ForegroundColor Green
                }
            }
        }
        
        if ($solutionFilesUpdated -eq 0) {
            Write-Host "  No solution files needed updating" -ForegroundColor Yellow
        }
    }
    
    Write-Host "  Updated $solutionFilesUpdated solution file(s)" -ForegroundColor Green
    Write-Host ""
    
    # Summary
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "RENAME OPERATION COMPLETED" -ForegroundColor Green
    Write-Host "="*80 -ForegroundColor Cyan
    Write-Host "Project file renamed: 1" -ForegroundColor Green
    Write-Host "ProjectReferences updated: $projectReferencesUpdated" -ForegroundColor Green
    Write-Host "InternalsVisibleTo attributes updated: $internalsVisibleToUpdated" -ForegroundColor Green
    Write-Host "Solution files updated: $solutionFilesUpdated" -ForegroundColor Green
    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Build the solution to verify all references are correct" -ForegroundColor White
    Write-Host "  2. Run tests to ensure everything still works" -ForegroundColor White
    Write-Host "  3. Consider running C-Validate-SolutionStructure" -ForegroundColor White
    Write-Host ""
}
