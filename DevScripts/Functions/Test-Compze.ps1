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
    
    .PARAMETER SingleThreadedTesting
    Run tests single-threaded (forces sequential test execution, useful for debugging)
    
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
        [switch]$Clean,
        [switch]$SingleThreadedTesting
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        if ($Clean) {
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
