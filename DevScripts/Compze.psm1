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
    Use -Build to build before testing with single-threaded build to avoid race conditions.
    
    .PARAMETER Build
    Build the solution before running tests (single-threaded build)
    
    .PARAMETER SingleThreadedTesting
    Run tests single-threaded (forces sequential test execution)
    
    .EXAMPLE
    Test-Compze
    Runs all tests without building (parallel)
    
    .EXAMPLE
    Test-Compze -Build
    Builds (single-threaded) then runs all tests (parallel)
    
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
            dotnet build $solutionPath -m:1
            
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
        Write-Host "✓ Reload complete!" -ForegroundColor Green
    } else {
        Write-Host "✓ Compze module reloaded!" -ForegroundColor Green
    }
}

# Export the functions
Export-ModuleMember -Function @(
    'Fix-CsprojExclusions',
    'Remove-RedundantInternalsVisibleTo', 
    'Validate-SolutionStructure',
    'Test-Compze',
    'Reload-Profile'
)
