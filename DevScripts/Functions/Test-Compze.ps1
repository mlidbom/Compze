function Test-Compze {
    <#
    .SYNOPSIS
    Runs Compze tests with proper configuration
    
    .DESCRIPTION
    Runs the full Compze test suite. By default, runs tests without building (assumes already built).
    Tests run in parallel according to assembly-level attributes by default.
    
    .PARAMETER Build
    Build the solution before running tests
    
    .PARAMETER Clean
    Performs a deep clean and build before running tests (implies -Build)
    
    .PARAMETER FullGitReset
    Performs a full git reset that removes all untracked files and directories before testing.
    This will backup TestUsingPluggableComponentCombinations before running git clean.
    Requires a clean working tree (no uncommitted changes). Implies -Clean and -Build.
    
    .PARAMETER SingleThreadedTesting
    Run tests single-threaded (forces sequential test execution, useful for debugging)
    
    .PARAMETER WhatIf
    Shows what would be deleted by git clean without actually deleting anything (only applies with -FullGitReset).
    
    .EXAMPLE
    Test-Compze
    Runs all tests without building (parallel)
    
    .EXAMPLE
    Test-Compze -Build
    Builds then runs all tests (parallel)
    
    .EXAMPLE
    Test-Compze -Clean
    Cleans, builds, then runs all tests (parallel)
    
    .EXAMPLE
    Test-Compze -FullGitReset
    Performs full git clean, builds, then runs all tests (parallel)
    
    .EXAMPLE
    Test-Compze -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    
    .EXAMPLE
    Test-Compze -SingleThreadedTesting
    Runs all tests single-threaded without building (for debugging)
    
    .EXAMPLE
    Test-Compze -Build -SingleThreadedTesting
    Builds then runs all tests single-threaded (for debugging)
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Build,
        [switch]$Clean,
        [switch]$FullGitReset,
        [switch]$SingleThreadedTesting
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        if ($FullGitReset) {
            if ($WhatIfPreference) {
                Clean-Compze -FullGitReset -WhatIf
                return
            } else {
                Clean-Compze -FullGitReset
            }
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return
            }
            
            if ($SingleThreadedTesting) {
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                dotnet test $solutionPath --no-build
            }
        }
        elseif ($Clean) {
            Clean-Compze
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return
            }
            
            if ($SingleThreadedTesting) {
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                dotnet test $solutionPath --no-build
            }
        }
        elseif ($Build) {
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return
            }
            
            if ($SingleThreadedTesting) {
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                dotnet test $solutionPath --no-build
            }
        } else {
            if ($SingleThreadedTesting) {
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                dotnet test $solutionPath --no-build
            }
        }
    } finally {
        Pop-Location
    }
}
