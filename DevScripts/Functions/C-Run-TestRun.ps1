# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Run-TestRun {
    <#
    .SYNOPSIS
    Runs a single test run and returns detailed results
    
    .DESCRIPTION
    Executes the test suite once and returns structured information about the results.
    This is a low-level helper function used by C-Test and C-Test-Commit.
    
    .PARAMETER SolutionPath
    Path to the solution file to test
    
    .PARAMETER SingleThreaded
    Runs one test at a time: the test assemblies one after another (MSBuild's -m:1), and within each, no test collections in
    parallel (xUnit's ParallelizeTestCollections=false, as a RunSettings argument, which the VSTest runner honours). Slower
    to start too: MSBuild then goes through the solution's projects one at a time.
    
    .PARAMETER MaxParallelAssemblies
    Runs at most this many test assemblies at once (MSBuild's -m). Ignored with SingleThreaded.

    .PARAMETER MaxParallelThreads
    Runs at most this many test collections at once within each test assembly (xUnit's MaxParallelThreads, as a RunSettings
    argument). Ignored with SingleThreaded.

    .PARAMETER KeepResultsIn
    Moves the run's TRX files into a folder of their own under this folder once counted, where by default they are deleted.

    .PARAMETER Filter
    Runs only the tests that match this filter, passed to dotnet test --filter, such as "FullyQualifiedName~CQRS.TaggregateStores"

    .PARAMETER DisplayOutput
    Whether to display the test output to the console (default: $true)
    
    .OUTPUTS
    PSCustomObject with properties:
    - TestOutput: Raw test output lines
    - Failed: Number of failed tests
    - Succeeded: Number of passed tests
    - Skipped: Number of tests not run: skipped, or explicit and not asked for
    - Executed: Total number of tests, run or not
    - ExitCode: The exit code from dotnet test
    - ElapsedSeconds: Time taken to run tests (rounded to 1 decimal)
    - FailedTests: Array of failed test objects with FullName and DisplayName properties
    - AbortedRuns: Number of test assembly runs that aborted, which report no failed test
    - AssembliesWithNoTestHost: File names of the test assemblies that aborted because no test host could be found for them
    
    .EXAMPLE
    $result = C-Run-TestRun -SolutionPath "C:\Dev\Compze\Compze.AllProjects.slnx"
    if ($result.Failed -gt 0) { Write-Host "Tests failed!" }
    
    .EXAMPLE
    $result = C-Run-TestRun -SolutionPath $solutionPath -SingleThreaded -DisplayOutput:$false
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath,
        
        [switch]$SingleThreaded,

        [int]$MaxParallelAssemblies,

        [int]$MaxParallelThreads,

        [string]$KeepResultsIn,

        [string]$Filter,

        [bool]$DisplayOutput = $true
    )

    # Run tests and capture output. Each test assembly also writes its results to a TRX file, which the counts are read from:
    # test hosts running side by side can write their summaries onto one console line, which a count from the console misses.
    $resultsDirectory = Join-Path ([IO.Path]::GetTempPath()) "compze-test-results-$([guid]::NewGuid())"
    $testArguments = @('test', $SolutionPath, '--no-build', '--logger', 'trx', '--results-directory', $resultsDirectory)
    if ($Filter) {
        $testArguments += @('--filter', $Filter)
    }
    # RunSettings arguments go last, after --.
    if ($SingleThreaded) {
        $testArguments += @('-m:1', '--', 'xUnit.ParallelizeTestCollections=false')
    } else {
        if ($MaxParallelAssemblies -gt 0) { $testArguments += "-m:$MaxParallelAssemblies" }
        if ($MaxParallelThreads -gt 0) { $testArguments += @('--', "xUnit.MaxParallelThreads=$MaxParallelThreads") }
    }
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $testOutput = dotnet @testArguments 2>&1
    $stopwatch.Stop()
    $exitCode = $LASTEXITCODE

    # Display output if requested
    if ($DisplayOutput) {
        $testOutput | C-Filter-TestOutput | ForEach-Object { Write-Host $_ }
    }
    
    # Count the results
    $failed = 0
    $succeeded = 0
    $skipped = 0
    $executed = 0
    
    # urgent: When a test assembly's host crashes, a stack overflow for instance, its summary line can still read "Passed!",
    #   with fewer tests in it than the assembly has. C-Test fails the run on dotnet test's exit code, and counts the runs that
    #   printed "Test Run Aborted", naming the assembly when its test host couldn't be found. Name a crashed host's assembly in
    #   the failure too (Magnus, 2026-11-16).
    # A TRX file's path is read literally: VSTest names a file whose name is taken with a suffix such as "[1]", which a wildcard
    # path would read as a character class.
    if (Test-Path -LiteralPath $resultsDirectory) {
        foreach ($resultsFile in Get-ChildItem -LiteralPath $resultsDirectory -Filter *.trx) {
            [xml]$results = Get-Content -LiteralPath $resultsFile.FullName -Raw
            foreach ($result in $results.TestRun.Results.UnitTestResult) {
                $executed++
                switch ($result.outcome) {
                    'Passed' { $succeeded++ }
                    'NotExecuted' { $skipped++ }
                    default { $failed++ }
                }
            }
        }
        if ($KeepResultsIn) {
            New-Item -ItemType Directory -Force -Path $KeepResultsIn | Out-Null
            Copy-Item -LiteralPath $resultsDirectory -Destination $KeepResultsIn -Recurse
        }
        Remove-Item -LiteralPath $resultsDirectory -Recurse -Force
    }

    # An aborted test assembly run reports no failed test: its host couldn't start, or crashed.
    $abortedRuns = @($testOutput | Where-Object { "$_" -match '^Test Run Aborted' }).Count
    $assembliesWithNoTestHost = @($testOutput | ForEach-Object { if ("$_" -match "^Could not find testhost for test source '(.+)'\.") { [System.IO.Path]::GetFileName($Matches[1]) } })

    # Extract failed test information
    $failedTests = @()
    if ($failed -gt 0) {
        $failedTests = Get-FailedTestsFromOutput -TestOutput $testOutput
    }
    
    # Return structured result
    return [PSCustomObject]@{
        TestOutput = $testOutput
        Failed = $failed
        Succeeded = $succeeded
        Skipped = $skipped
        Executed = $executed
        ExitCode = $exitCode
        ElapsedSeconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
        FailedTests = $failedTests
        AbortedRuns = $abortedRuns
        AssembliesWithNoTestHost = $assembliesWithNoTestHost
    }
}
