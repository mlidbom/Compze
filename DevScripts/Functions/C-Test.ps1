# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Test {
    <#
    .SYNOPSIS
    Runs Compze tests with proper configuration
    
    .DESCRIPTION
    Runs the full Compze test suite, or with -Filter the tests that match, on the combinations of pluggable components in
    src/TestUsingPluggableComponentCombinations, or with -Combinations on those. By default, builds the solution before running tests.
    Tests run in parallel according to assembly-level attributes by default.
    
    Sets $LASTEXITCODE to 0 on success and 1 on failure, and reports a failure as an error of its own, so that
    `pwsh -Command "...; C-Test"` exits 1 too. A test run fails when dotnet test does, which includes a test process
    that died without reporting a failed test.
    
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

    .PARAMETER MaxParallelAssemblies
    Runs at most this many test assemblies at once (MSBuild's -m), where by default it runs one for each logical processor.

    .PARAMETER MaxParallelThreads
    Runs at most this many test collections at once within each test assembly (xUnit's MaxParallelThreads), where by
    default xUnit runs one for each logical processor. 0 leaves it to xUnit.

    By default 8 when the run includes a MySql combination, and xUnit's otherwise. MySQL's DDL is bound by its syncs to
    disk, and its throughput stops rising at about 8 statements at once: with a test class per logical processor, the
    DbPool's resets of its databases take seconds, tests run past their reservations, and two runs at once exhaust the
    server's connections. At 8, the MySql suite runs fastest and passes on threadripper's server. The limit is per
    assembly, so it holds for every combination in such a run. Measured in
    src/Compze.DbPool.MySql/dev_docs/why-resets-take-seconds-on-mysql.md (Magnus, 2026-11-27).

    .PARAMETER KeepResultsIn
    Keeps each test run's TRX files, the test results with each failure's message and stack trace, in a folder of its own
    under this folder, where by default they are deleted once counted.

    .PARAMETER Iterations
    Run the test suite multiple times and display a summary of results across all iterations

    .PARAMETER Combinations
    Runs the tests on these combinations of pluggable components instead of those in
    src/TestUsingPluggableComponentCombinations: one or more, each written as a line of that file, such as
    'MsSql:Microsoft:Newtonsoft:AspNetCore'. A * stands for every value of its dimension: '*:Microsoft:Newtonsoft:AspNetCore'.

    The tests read the copy of that file that the build puts in each test project's output folder. So after building, it
    writes the combinations into every copy, and once the run is over restores each copy to exactly what it was, also when
    the run fails or is interrupted. The file itself is never changed, and it works with -NoBuild too. A run killed before
    it restores the copies leaves a record of them, and the next C-Test or C-Build restores them from it.

    .PARAMETER Filter
    Runs only the tests that match this filter, passed to dotnet test --filter, such as "FullyQualifiedName~CQRS.TaggregateStores".
    A filter that matches no test fails the run.

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

    .EXAMPLE
    C-Test -Combinations 'PgSql:Microsoft:Newtonsoft:AspNetCore' -Filter "FullyQualifiedName~CQRS.TaggregateStores"
    Builds, then runs the taggregate store tests on PostgreSQL only

    .EXAMPLE
    C-Test -NoBuild -Combinations 'MsSql:Microsoft:Newtonsoft:AspNetCore', 'MySql:Microsoft:Newtonsoft:AspNetCore'
    Runs all tests, without building, on SQL Server and on MySQL
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [switch]$NoBuild,
        [switch]$Clean,
        [switch]$FullGitReset,
        [switch]$SingleThreadedTesting,
        [int]$MaxParallelAssemblies,
        [int]$MaxParallelThreads,
        [string]$KeepResultsIn,
        [int]$Iterations = 1,
        [string[]]$Combinations,
        [string]$Filter
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
        Restore-CombinationsFileCopies -WarnIfAnyAreRestored

        # Build and validate
        if (-not (Invoke-BuildWithValidation -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset)) {
            if ($LASTEXITCODE -ne 0) {
                Write-CommandFailure -Command $PSCmdlet -Message "The build failed, so no tests ran."
            }
            return
        }

        if ($Combinations) {
            Set-CombinationsFileCopies -Combinations $Combinations
        }

        $runsMySql = @(if ($Combinations) { $Combinations } else { Get-ActiveCombinations }) | Where-Object { $_ -match '^\s*(MySql|\*):' }
        if (-not $PSBoundParameters.ContainsKey('MaxParallelThreads') -and $runsMySql) {
            $MaxParallelThreads = 8
        }

        # Run tests for each iteration
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n=== Running test iteration $i of $Iterations ===" -ForegroundColor Cyan
            }
            
            # Run tests
            $testRunResult = C-Run-TestRun -SolutionPath $script:CompzeSolutionPath -SingleThreaded:$SingleThreadedTesting -MaxParallelAssemblies $MaxParallelAssemblies -MaxParallelThreads $MaxParallelThreads -KeepResultsIn $KeepResultsIn -Filter $Filter
            
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
        
        # A test run fails when dotnet test does: also when a test process dies without reporting a failed test.
        $failedRuns = @($iterationResults | Where-Object { $_.ExitCode -ne 0 })
        $totalFailedTests = ($iterationResults | Measure-Object -Property Failed -Sum).Sum
        if ($totalFailedTests -gt 0 -and $Iterations -eq 1) {
            Show-FailedTestsSummary -FailedTests $allFailedTests
        }
        if ($Filter -and ($iterationResults | Measure-Object -Property Executed -Sum).Sum -eq 0) {
            Write-CommandFailure -Command $PSCmdlet -Message "The filter $Filter matched no test."
        } elseif ($failedRuns.Count -gt 0) {
            Write-CommandFailure -Command $PSCmdlet -Message "$($failedRuns.Count) of $Iterations test run(s) failed, with $totalFailedTests failed test(s)$(Get-AbortedTestRunsDescription -TestRunResults $iterationResults)."
        } else {
            $global:LASTEXITCODE = 0
        }
    } finally {
        # Also when the run failed, or was interrupted with Ctrl+C. A killed run leaves the record for the next C-Test or C-Build.
        Restore-CombinationsFileCopies
        Pop-Location
    }
}
