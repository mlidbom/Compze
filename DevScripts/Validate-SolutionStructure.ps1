# Validation script for solution structure rules
# Rules:
# 1. Project file name pattern: Compze.A.B.C.csproj should be in Compze/A/B/C/
# 2. Nested projects one level down: Compze/A/B/C/ may contain Compze.A.B.C.D.csproj in Compze/A/B/C/D/
# 3. File system structure to filename matching: Compze.A.B.C.D.csproj must be in directory Compze/A/B/C/D/

# Determine the workspace root (parent of DevScripts folder)
$WorkspaceRoot = Split-Path -Parent $PSScriptRoot
$srcRoot = "$WorkspaceRoot\src"
$violations = @()

# Get all .csproj files in Compze directory
$projects = Get-ChildItem -Path "$srcRoot\Compze" -Recurse -Filter "*.csproj"

foreach ($project in $projects) {
    $projectName = $project.BaseName
    # Use PowerShell's -ireplace for case-insensitive replacement
    $dirPath = $project.DirectoryName
    # Remove the src root path (case-insensitive) and convert to forward slashes
    $relativePath = $dirPath.Substring($srcRoot.Length + 1).Replace("\", "/")
    
    # Expected path based on project name
    # Convert Compze.A.B.C.D to Compze/A/B/C/D
    # Split by dots and join with slashes
    $expectedPath = $projectName.Replace(".", "/")
    
    if ($relativePath -ne $expectedPath) {
        $violations += [PSCustomObject]@{
            ProjectFile = $project.Name
            ActualPath = $relativePath
            ExpectedPath = $expectedPath
            FullPath = $project.FullName.Substring($srcRoot.Length + 1)
        }
    }
}

Write-Host "`n========================================" -ForegroundColor Cyan
Write-Host "Solution Structure Validation Results" -ForegroundColor Cyan
Write-Host "========================================`n" -ForegroundColor Cyan

if ($violations.Count -eq 0) {
    Write-Host "✓ No violations found! All projects follow the naming convention." -ForegroundColor Green
} else {
    Write-Host "✗ Found $($violations.Count) violation(s):`n" -ForegroundColor Red
    
    foreach ($violation in $violations) {
        Write-Host "Project: $($violation.ProjectFile)" -ForegroundColor Yellow
        Write-Host "  Actual:   $($violation.ActualPath)/" -ForegroundColor Red
        Write-Host "  Expected: $($violation.ExpectedPath)/" -ForegroundColor Green
        Write-Host "  Full Path: $($violation.FullPath)" -ForegroundColor Gray
        Write-Host ""
    }
}

Write-Host "`nTotal projects checked: $($projects.Count)" -ForegroundColor Cyan
Write-Host "Violations found: $($violations.Count)`n" -ForegroundColor Cyan
