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
    Run tests single-threaded (forces sequential test execution)
    
    .PARAMETER DisplayOutput
    Whether to display the test output to the console (default: $true)
    
    .OUTPUTS
    PSCustomObject with properties:
    - TestOutput: Raw test output lines
    - Failed: Number of failed tests
    - Succeeded: Number of passed tests
    - Skipped: Number of skipped tests
    - Executed: Total number of tests executed
    - ExitCode: The exit code from dotnet test
    - ElapsedSeconds: Time taken to run tests (rounded to 1 decimal)
    - FailedTests: Array of failed test objects with FullName and DisplayName properties
    
    .EXAMPLE
    $result = C-Run-TestRun -SolutionPath "C:\Dev\Compze\src\Compze.AllProjects.slnx"
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
        
        [bool]$DisplayOutput = $true
    )
    
    # Run tests and capture output
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    if ($SingleThreaded) {
        $testOutput = dotnet test $SolutionPath --no-build 2>&1
    } else {
        $testOutput = dotnet test $SolutionPath --no-build 2>&1
    }
    $stopwatch.Stop()
    $exitCode = $LASTEXITCODE
    
    # Display output if requested
    if ($DisplayOutput) {
        $testOutput | C-Filter-TestOutput | ForEach-Object { Write-Host $_ }
    }
    
    # Parse test results
    $failed = 0
    $succeeded = 0
    $skipped = 0
    $executed = 0
    
    $summaryLines = $testOutput | Where-Object { $_ -match '(Passed!|Failed!)\s+-\s+Failed:' }
    
    if ($summaryLines) {
        foreach ($line in $summaryLines) {
            $summaryText = $line.ToString()
            
            if ($summaryText -match 'Failed:\s*(\d+)') {
                $failed += [int]$matches[1]
            }
            if ($summaryText -match 'Passed:\s*(\d+)') {
                $succeeded += [int]$matches[1]
            }
            if ($summaryText -match 'Skipped:\s*(\d+)') {
                $skipped += [int]$matches[1]
            }
            if ($summaryText -match 'Total:\s*(\d+)') {
                $executed += [int]$matches[1]
            }
        }
    }
    
    # If parsing failed, try to calculate from what we have
    if ($executed -eq 0 -and ($succeeded -gt 0 -or $failed -gt 0)) {
        $executed = $succeeded + $failed + $skipped
    }
    
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
    }
}
