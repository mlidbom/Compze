# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Validate-SolutionStructure {
    <#
    .SYNOPSIS
    Validates the Compze solution structure
    
    .DESCRIPTION
    Validates that all projects follow the flat-layout naming convention:
    1. Library projects: src/<ProjectName>/<ProjectName>.csproj
    2. Test projects: test/<ProjectName>/<ProjectName>.csproj
    3. Each project has its own top-level directory (no nesting)
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    $srcRoot = Join-Path $script:CompzeRoot "src"
    $testRoot = Join-Path $script:CompzeRoot "test"
    $violations = @()

    # Collect all project files from src/ and test/
    $projects = @()
    $projects += Get-ProjectFilesInPath -Path $srcRoot
    if (Test-Path $testRoot) {
        $projects += Get-ProjectFilesInPath -Path $testRoot
    }

    foreach ($project in $projects) {
        $projectName = $project.BaseName
        $dirPath = $project.DirectoryName
        $dirName = Split-Path -Leaf $dirPath
        
        # Skip excluded directories (Samples, Websites, SolutionStructure, msbuild, DevScripts, TODO)
        $relativePath = $dirPath.Substring($script:CompzeRoot.Length + 1).Replace("\", "/")
        if ($relativePath -match '^src/Samples' -or 
            $relativePath -match '^src/Websites' -or 
            $relativePath -match '^src/SolutionStructure' -or 
            $relativePath -match '^src/msbuild' -or
            $relativePath -match '^src/TODO' -or
            $relativePath -match '^DevScripts') {
            continue
        }
        
        # The directory name must match the project name
        if ($dirName -ne $projectName) {
            $violations += [PSCustomObject]@{
                ProjectFile = $project.Name
                ActualPath = $relativePath
                ExpectedPath = if ($relativePath -match '^test/') { "test/$projectName" } else { "src/$projectName" }
                FullPath = $project.FullName.Substring($script:CompzeRoot.Length + 1)
                Issue = "Directory name '$dirName' doesn't match project name '$projectName'"
            }
            continue
        }
        
        # The parent directory must be src/ or test/ (flat layout - no nesting)
        $parentDir = Split-Path -Parent $dirPath
        $parentDirName = Split-Path -Leaf $parentDir
        $isInSrc = $parentDir.TrimEnd('\') -eq $srcRoot.TrimEnd('\')
        $isInTest = $parentDir.TrimEnd('\') -eq $testRoot.TrimEnd('\')
        
        if (-not $isInSrc -and -not $isInTest) {
            $violations += [PSCustomObject]@{
                ProjectFile = $project.Name
                ActualPath = $relativePath
                ExpectedPath = if ($relativePath -match '^test/' -or $projectName -match '\.Tests') { "test/$projectName" } else { "src/$projectName" }
                FullPath = $project.FullName.Substring($script:CompzeRoot.Length + 1)
                Issue = "Project is nested under '$parentDirName' instead of being directly under src/ or test/"
            }
        }
    }

    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Solution Structure Validation Results" -ForegroundColor Cyan
    Write-Host "========================================`n" -ForegroundColor Cyan

    if ($violations.Count -eq 0) {
        Write-Host "No violations found! All projects follow the flat-layout convention." -ForegroundColor Green
    } else {
        Write-Host "Found $($violations.Count) violation(s):`n" -ForegroundColor Red
        
        foreach ($violation in $violations) {
            Write-Host "Project: $($violation.ProjectFile)" -ForegroundColor Yellow
            Write-Host "  Actual:   $($violation.ActualPath)/" -ForegroundColor Red
            Write-Host "  Expected: $($violation.ExpectedPath)/" -ForegroundColor Green
            Write-Host "  Issue:    $($violation.Issue)" -ForegroundColor Gray
            Write-Host ""
        }
    }

    Write-Host "`nTotal projects checked: $($projects.Count)" -ForegroundColor Cyan
    Write-Host "Violations found: $($violations.Count)`n" -ForegroundColor Cyan
}
