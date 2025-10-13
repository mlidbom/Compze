function C-Test {
    <#
    .SYNOPSIS
    Runs Compze tests with proper configuration
    
    .DESCRIPTION
    Runs the full Compze test suite. By default, builds the solution before running tests.
    Tests run in parallel according to assembly-level attributes by default.
    
    .PARAMETER NoBuild
    Skip building the solution before running tests (assumes already built)
    
    .PARAMETER Clean
    Performs a deep clean and build before running tests
    
    .PARAMETER FullGitReset
    Performs a full git reset that removes all untracked files and directories before testing.
    This will backup TestUsingPluggableComponentCombinations before running git clean.
    Requires a clean working tree (no uncommitted changes). Implies -Clean.
    
    .PARAMETER SingleThreadedTesting
    Run tests single-threaded (forces sequential test execution, useful for debugging)
    
    .PARAMETER WhatIf
    Shows what would be deleted by git clean without actually deleting anything (only applies with -FullGitReset).
    
    .EXAMPLE
    C-Test
    Builds then runs all tests (parallel)
    
    .EXAMPLE
    C-Test -NoBuild
    Runs all tests without building (parallel)
    
    .EXAMPLE
    C-Test -Clean
    Cleans, builds, then runs all tests (parallel)
    
    .EXAMPLE
    C-Test -FullGitReset
    Performs full git clean, builds, then runs all tests (parallel)
    
    .EXAMPLE
    C-Test -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    
    .EXAMPLE
    C-Test -SingleThreadedTesting
    Builds then runs all tests single-threaded (for debugging)
    
    .EXAMPLE
    C-Test -NoBuild -SingleThreadedTesting
    Runs all tests single-threaded without building (for debugging)
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset,
        [switch]$SingleThreadedTesting
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        if ($FullGitReset) {
            if ($WhatIfPreference) {
                C-Clean -FullGitReset -WhatIf
                return
            } else {
                C-Clean -FullGitReset
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
            C-Clean
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
        elseif ($NoBuild) {
            if ($SingleThreadedTesting) {
                dotnet test $solutionPath --no-build -- NUnit.NumberOfTestWorkers=0
            } else {
                dotnet test $solutionPath --no-build
            }
        } else {
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
    } finally {
        Pop-Location
    }
}
