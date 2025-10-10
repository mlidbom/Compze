# Compze Development Module
# This module provides convenient commands for Compze development

$script:CompzeRoot = Split-Path -Parent $PSScriptRoot

# Import the DevScripts as functions
function Fix-CsprojExclusions {
    <#
    .SYNOPSIS
    Fixes .csproj exclusions for Compze projects
    
    .DESCRIPTION
    Runs the Fix-CsprojExclusions.ps1 script from any directory
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    & "$PSScriptRoot\Fix-CsprojExclusions.ps1" @args
}

function Remove-RedundantInternalsVisibleTo {
    <#
    .SYNOPSIS
    Removes redundant InternalsVisibleTo attributes
    
    .DESCRIPTION
    Runs the Remove-RedundantInternalsVisibleTo.ps1 script from any directory
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    & "$PSScriptRoot\Remove-RedundantInternalsVisibleTo.ps1" @args
}

function Validate-SolutionStructure {
    <#
    .SYNOPSIS
    Validates the Compze solution structure
    
    .DESCRIPTION
    Runs the Validate-SolutionStructure.ps1 script from any directory
    #>
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    & "$PSScriptRoot\Validate-SolutionStructure.ps1" @args
}

function Test-Compze {
    <#
    .SYNOPSIS
    Runs Compze tests with proper configuration
    
    .DESCRIPTION
    Runs the full Compze test suite. By default, runs tests without building (assumes already built).
    Tests run in parallel according to assembly-level attributes by default.
    
    .PARAMETER Build
    Build the solution before running tests
    
    .PARAMETER SingleThreadedTesting
    Run tests single-threaded (forces sequential test execution, useful for debugging)
    
    .EXAMPLE
    Test-Compze
    Runs all tests without building (parallel)
    
    .EXAMPLE
    Test-Compze -Build
    Builds then runs all tests (parallel)
    
    .EXAMPLE
    Test-Compze -SingleThreadedTesting
    Runs all tests single-threaded without building (for debugging)
    
    .EXAMPLE
    Test-Compze -Build -SingleThreadedTesting
    Builds then runs all tests single-threaded (for debugging)
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Build,
        [switch]$SingleThreadedTesting
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        if ($Build) {
            Write-Host "Building solution..." -ForegroundColor Cyan
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return
            }
            
            if ($SingleThreadedTesting) {
                Write-Host "`nRunning tests (single-threaded)..." -ForegroundColor Cyan
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                Write-Host "`nRunning tests..." -ForegroundColor Cyan
                dotnet test $solutionPath --no-build
            }
        } else {
            if ($SingleThreadedTesting) {
                Write-Host "Running tests without building (single-threaded)..." -ForegroundColor Cyan
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                Write-Host "Running tests without building..." -ForegroundColor Cyan
                dotnet test $solutionPath --no-build
            }
        }
    } finally {
        Pop-Location
    }
}

function Fix-Encodings {
    <#
    .SYNOPSIS
    Converts files to UTF-8 without BOM encoding (standard for modern .NET)
    
    .DESCRIPTION
    Scans the specified path for git-tracked files and converts any that don't use UTF-8 without BOM to UTF-8 without BOM.
    This ensures consistent encoding across the codebase and prevents git diff issues. UTF-8 without BOM is the
    modern standard for .NET projects and provides better cross-platform compatibility.
    
    .PARAMETER Path
    The path to scan. Defaults to the src directory of the Compze repository.
    
    .PARAMETER FilePattern
    The file pattern to match. Defaults to *.cs
    
    .EXAMPLE
    Fix-Encodings
    Converts all git-tracked .cs files in src/ to UTF-8 without BOM
    
    .EXAMPLE
    Fix-Encodings -Path "src/Tests" -FilePattern "*.cs"
    Converts all git-tracked .cs files in src/Tests to UTF-8 without BOM
    
    .EXAMPLE
    Fix-Encodings -WhatIf
    Shows which files would be converted without actually converting them
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter()]
        [string]$Path = (Join-Path $script:CompzeRoot "src"),
        
        [Parameter()]
        [string]$FilePattern = "*.cs"
    )
    
    if (-not (Test-Path $Path)) {
        Write-Error "Path not found: $Path"
        return
    }
    
    Write-Host "Scanning for git-tracked files in: $Path" -ForegroundColor Cyan
    Write-Host "Pattern: $FilePattern" -ForegroundColor Cyan
    Write-Host "Target: UTF-8 without BOM (modern .NET standard)" -ForegroundColor Cyan
    Write-Host ""
    
    # Get all git-tracked files matching the pattern
    Push-Location $Path
    try {
        $gitFiles = git ls-files $FilePattern 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "Not a git repository or git not available. Falling back to all files."
            $files = Get-ChildItem -Path $Path -Recurse -Filter $FilePattern -File
        } else {
            $files = $gitFiles | ForEach-Object { Get-Item (Join-Path $Path $_) -ErrorAction SilentlyContinue } | Where-Object { $_ -ne $null }
        }
    } finally {
        Pop-Location
    }
    
    if ($files.Count -eq 0) {
        Write-Host "No matching files found." -ForegroundColor Yellow
        return
    }
    
    $utf8NoBom = New-Object System.Text.UTF8Encoding $false
    $convertedCount = 0
    $skippedCount = 0
    
    foreach ($file in $files) {
        try {
            # Read the file as bytes to check encoding
            $bytes = [System.IO.File]::ReadAllBytes($file.FullName)
            
            # Check if file has UTF-8 BOM (EF BB BF)
            $hasUtf8Bom = ($bytes.Length -ge 3 -and 
                          $bytes[0] -eq 0xEF -and 
                          $bytes[1] -eq 0xBB -and 
                          $bytes[2] -eq 0xBF)
            
            # Check if file is already UTF-8 without BOM
            # We'll convert if it has BOM, or if it appears to be another encoding
            $needsConversion = $hasUtf8Bom
            
            if ($needsConversion) {
                if ($PSCmdlet.ShouldProcess($file.FullName, "Convert to UTF-8 without BOM")) {
                    # Read content and write with UTF-8 without BOM
                    $content = [System.IO.File]::ReadAllText($file.FullName)
                    [System.IO.File]::WriteAllText($file.FullName, $content, $utf8NoBom)
                    Write-Host "Converted: $($file.FullName)" -ForegroundColor Green
                    $convertedCount++
                } else {
                    Write-Host "[WhatIf] Would convert: $($file.FullName)" -ForegroundColor Yellow
                    $convertedCount++
                }
            } else {
                $skippedCount++
            }
        } catch {
            Write-Warning "Failed to process $($file.FullName): $_"
        }
    }
    
    Write-Host ""
    if ($WhatIfPreference) {
        Write-Host "Summary (WhatIf mode):" -ForegroundColor Cyan
        Write-Host "  Would convert: $convertedCount files" -ForegroundColor Yellow
    } else {
        Write-Host "Summary:" -ForegroundColor Cyan
        Write-Host "  Converted: $convertedCount files" -ForegroundColor Green
    }
    Write-Host "  Already UTF-8 without BOM: $skippedCount files" -ForegroundColor Gray
    Write-Host "  Total scanned: $($files.Count) files" -ForegroundColor Cyan
}

function Reload-Profile {
    <#
    .SYNOPSIS
    Reloads the PowerShell profile
    
    .DESCRIPTION
    Reloads your PowerShell profile without restarting the shell.
    Also force-reloads the Compze module to pick up any changes.
    
    .EXAMPLE
    Reload-Profile
    Reloads your profile and the Compze module
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    Write-Host "Reloading Compze module..." -ForegroundColor Cyan
    Import-Module (Join-Path $PSScriptRoot "Compze.psd1") -DisableNameChecking -Force -Global
    
    if (Test-Path $PROFILE) {
        Write-Host "Reloading PowerShell profile..." -ForegroundColor Cyan
        . $PROFILE
        Write-Host "Reload complete!" -ForegroundColor Green
    } else {
        Write-Host "Compze module reloaded!" -ForegroundColor Green
    }
}

# Export the functions
Export-ModuleMember -Function @(
    'Fix-CsprojExclusions',
    'Remove-RedundantInternalsVisibleTo', 
    'Validate-SolutionStructure',
    'Test-Compze',
    'Fix-Encodings',
    'Reload-Profile'
)
