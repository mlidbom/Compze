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
    
    .PARAMETER Iterations
    Run the test suite multiple times and display a summary of results across all iterations
    
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
    
    .EXAMPLE
    C-Test -Iterations 10
    Runs the test suite 10 times and displays a summary of results
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset,
        [switch]$SingleThreadedTesting,
        [int]$Iterations = 1
    )
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    
    # Array to store results from each iteration
    $iterationResults = @()
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        # Build if needed
        C-Build -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset
        
        # Handle -WhatIf for FullGitReset (returns early after showing what would be deleted)
        if ($FullGitReset -and $WhatIfPreference) {
            return
        }
        
        # Check if build failed
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Build failed!"
            return
        }
        
        # Run tests for each iteration
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n=== Running test iteration $i of $Iterations ===" -ForegroundColor Cyan
            }
            
            # Run tests
            $testRunResult = C-Run-TestRun -SolutionPath $solutionPath -SingleThreaded:$SingleThreadedTesting
            
            # Create result object for tracking
            $result = [PSCustomObject]@{
                Iteration = $i
                Executed = $testRunResult.Executed
                Failed = $testRunResult.Failed
                Succeeded = $testRunResult.Succeeded
                Skipped = $testRunResult.Skipped
                ExitCode = $testRunResult.ExitCode
                ElapsedSeconds = $testRunResult.ElapsedSeconds
            }
            
            # Display iteration summary
            $cumulativeFailures = ($iterationResults + $result | Measure-Object -Property Failed -Sum).Sum
            if ($Iterations -gt 1) {
                Write-Host "Iteration $i failures: $($result.Failed) (cumulative: $cumulativeFailures) elapsed: $($result.ElapsedSeconds) seconds" -ForegroundColor $(if ($result.Failed -gt 0) { "Yellow" } else { "Green" })
            } else {
                Write-Host "Failures: $($result.Failed) elapsed: $($result.ElapsedSeconds) seconds" -ForegroundColor $(if ($result.Failed -gt 0) { "Yellow" } else { "Green" })
            }
            
            $iterationResults += $result
        }
        
        # Display summary if multiple iterations
        if ($Iterations -gt 1) {
            Write-Host "`n=== Test Iteration Summary ===" -ForegroundColor Cyan
            foreach ($result in $iterationResults) {
                $runNumber = $result.Iteration.ToString().PadLeft(2, '0')
                $status = if ($result.ExitCode -eq 0) { "SUCCESS" } else { "FAILURE" }
                $statusColor = if ($result.ExitCode -eq 0) { "Green" } else { "Red" }
                
                Write-Host "Run $runNumber`: " -NoNewline
                Write-Host "executed: $($result.Executed), " -NoNewline
                Write-Host "failed: $($result.Failed), " -NoNewline -ForegroundColor $(if ($result.Failed -gt 0) { "Red" } else { "White" })
                Write-Host "succeeded: $($result.Succeeded), " -NoNewline -ForegroundColor Green
                Write-Host "skipped: $($result.Skipped) - " -NoNewline
                Write-Host $status -ForegroundColor $statusColor
            }
            
            $successfulRuns = ($iterationResults | Where-Object { $_.ExitCode -eq 0 }).Count
            $failedRuns = ($iterationResults | Where-Object { $_.ExitCode -ne 0 }).Count
            
            Write-Host "`nRan $Iterations test runs. " -NoNewline
            Write-Host "$successfulRuns succeeded" -NoNewline -ForegroundColor Green
            Write-Host ", " -NoNewline
            Write-Host "$failedRuns failed" -ForegroundColor $(if ($failedRuns -gt 0) { "Red" } else { "Green" })
        }
    } finally {
        Pop-Location
    }
}
