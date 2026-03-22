# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Test {
    <#
    .SYNOPSIS
    Runs Compze tests with proper configuration
    
    .DESCRIPTION
    Runs the full Compze test suite. By default, builds the solution before running tests.
    Tests run in parallel according to assembly-level attributes by default.
    
    Returns exit code 0 on success, 1 on failure.
    
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
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset,
        [switch]$SingleThreadedTesting,
        [int]$Iterations = 1
    )
    
    # Array to store results from each iteration
    $iterationResults = @()
    $allFailedTests = @()
    
    # Default to stress-test-only mode for performance tests (skip timing assertions, keep stress testing)
    if (-not $env:COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY) {
        $env:COMPOSABLE_PERFORMANCE_TESTS_STRESS_TEST_ONLY = "true"
    }

    Push-Location $script:CompzeSrcRoot
    try {
        # Build and validate
        if (-not (Invoke-BuildWithValidation -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset)) {
            return
        }
        
        # Run tests for each iteration
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n=== Running test iteration $i of $Iterations ===" -ForegroundColor Cyan
            }
            
            # Run tests
            $testRunResult = C-Run-TestRun -SolutionPath $script:CompzeSolutionPath -SingleThreaded:$SingleThreadedTesting
            
            # Collect failed tests from this iteration
            if ($testRunResult.FailedTests -and $testRunResult.FailedTests.Count -gt 0) {
                $allFailedTests += $testRunResult.FailedTests
            }
            
            # Add iteration number to result
            $result = $testRunResult | Add-Member -NotePropertyName Iteration -NotePropertyValue $i -PassThru
            
            # Display iteration summary
            $cumulativeFailures = ($iterationResults + $result | Measure-Object -Property Failed -Sum).Sum
            Show-TestIterationSummary -IterationNumber $i -TotalIterations $Iterations -Failed $result.Failed -Passed $result.Succeeded -Skipped $result.Skipped -Total $result.Executed -CumulativeFailures $cumulativeFailures -ElapsedSeconds $result.ElapsedSeconds
            
            # Display failed tests from this iteration if any
            if ($result.Failed -gt 0 -and $result.FailedTests.Count -gt 0) {
                Show-FailedTestsSummary -FailedTests $result.FailedTests
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
            
            # Show summary of all unique failed tests across all iterations
            if ($allFailedTests.Count -gt 0) {
                Write-Host "`n=== All Unique Failed Tests Across All Iterations ===" -ForegroundColor Red
                $uniqueFailedTests = $allFailedTests | Select-Object -Property FullName -Unique
                foreach ($test in $uniqueFailedTests) {
                    Write-Host $test.FullName -ForegroundColor Red
                }
                Write-Host "`nTotal unique failed tests: $($uniqueFailedTests.Count)" -ForegroundColor Red
            }
        }
        
        # Set exit code based on whether any tests failed
        $totalFailedTests = ($iterationResults | Measure-Object -Property Failed -Sum).Sum
        if ($totalFailedTests -gt 0) {
            # For single iteration, show the failed tests
            if ($Iterations -eq 1) {
                Show-FailedTestsSummary -FailedTests $allFailedTests
            }
            $global:LASTEXITCODE = 1
        } else {
            $global:LASTEXITCODE = 0
        }
    } finally {
        Pop-Location
    }
}
