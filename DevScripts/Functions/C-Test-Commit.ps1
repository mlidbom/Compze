function C-Test-Commit {
    <#
    .SYNOPSIS
    Tests a commit by running the test suite multiple times with failure detection
    
    .DESCRIPTION
    Runs the test suite for the current commit with configurable failure detection.
    Useful for testing intermittent failures or as part of automated bisect processes.
    By default, builds the solution before running tests.
    Use -NoBuild to skip building (assumes already built).
    Use -Clean or -FullGitReset to clean and build before testing.
    
    Returns exit code 0 on success, 1 on failure.
    
    .PARAMETER FailureText
    Text that indicates a test failure. If this text appears in test output, exits with code 1.
    Case-insensitive substring match. Mutually exclusive with -MaxFailures.
    
    .PARAMETER MaxFailures
    Maximum cumulative test failures allowed across all iterations.
    If cumulative failures exceed this number, exits with code 1.
    Mutually exclusive with -FailureText.
    
    .PARAMETER NoBuild
    Skip building the solution before running tests (assumes already built)
    
    .PARAMETER Clean
    Performs a deep clean and build before running tests
    
    .PARAMETER FullGitReset
    Performs a full git reset that removes all untracked files and directories before testing.
    This will backup TestUsingPluggableComponentCombinations before running git clean.
    Requires a clean working tree (no uncommitted changes). Implies -Clean.
    
    .PARAMETER Iterations
    Number of times to run the test suite (default: 1).
    Useful for intermittent failures.
    
    .PARAMETER WhatIf
    Shows what would be deleted by git clean without actually deleting anything (only applies with -FullGitReset).
    
    .EXAMPLE
    C-Test-Commit -FailureText "System.NullReferenceException" -Iterations 5
    Runs tests 5 times, exits with code 1 if "System.NullReferenceException" appears in any run
    
    .EXAMPLE
    C-Test-Commit -MaxFailures 10 -Iterations 10
    Builds then runs tests 10 times, exits with code 1 if cumulative failures exceed 10
    
    .EXAMPLE
    C-Test-Commit -NoBuild -MaxFailures 5 -Iterations 3
    Runs tests 3 times without building, exits with code 1 if cumulative failures exceed 5
    
    .EXAMPLE
    C-Test-Commit -Clean -MaxFailures 5 -Iterations 3
    Cleans, builds, then runs tests 3 times, exits with code 1 if cumulative failures exceed 5
    
    .EXAMPLE
    C-Test-Commit -FullGitReset -MaxFailures 0
    Performs full git clean, builds, then runs tests once, exits with code 1 if any failures occur
    
    .EXAMPLE
    C-Test-Commit -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    
    .EXAMPLE
    C-Test-Commit -MaxFailures 5 -Iterations 3
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Commit is good!"
    } else {
        Write-Host "Commit has failures"
    }
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSAvoidGlobalVars', '')]
    param(
        [string]$FailureText,
        
        [int]$MaxFailures = 1,
        
        [switch]$NoBuild,
        
        [switch]$Clean,
        
        [switch]$FullGitReset,
        
        [int]$Iterations = 1
    )
    
    # Validate mutually exclusive parameters
    if ($FailureText -and $MaxFailures -ge 0) {
        Write-Error "Parameters -FailureText and -MaxFailures are mutually exclusive. Use one or the other."
        $global:LASTEXITCODE = 1
        return
    }
    
    $totalFailures = 0
    
    Push-Location $script:CompzeSrcRoot
    try {
        # Build and validate
        if (-not (Invoke-BuildWithValidation -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset)) {
            return
        }
        
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n=== Running test iteration $i of $Iterations ===" -ForegroundColor Cyan
            }
            
            # Run tests
            $result = C-Run-TestRun -SolutionPath $script:CompzeSolutionPath
            
            $totalFailures += $result.Failed
            
            # Display iteration summary
            Show-TestIterationSummary -IterationNumber $i -TotalIterations $Iterations -Failed $result.Failed -Passed $result.Succeeded -Skipped $result.Skipped -Total $result.Executed -CumulativeFailures $totalFailures -ElapsedSeconds $result.ElapsedSeconds
            
            # Check for FailureText if specified
            if ($FailureText) {
                $outputString = $result.TestOutput | Out-String
                if ($outputString -match [regex]::Escape($FailureText)) {
                    Write-Host "Found failure text: '$FailureText'" -ForegroundColor Red
                    $global:LASTEXITCODE = 1
                    return
                }
            }
            
            # Check for MaxFailures if specified
            if ($MaxFailures -ge 0) {
                if ($totalFailures -gt $MaxFailures) {
                    Write-Host "Cumulative failures ($totalFailures) exceeded MaxFailures ($MaxFailures)" -ForegroundColor Red
                    $global:LASTEXITCODE = 1
                    return
                }
            }
        }
        
        # All iterations passed - display final summary
        if ($Iterations -gt 1) {
            Write-Host "Total cumulative failures: $totalFailures" -ForegroundColor $(if ($totalFailures -eq 0) { "Green" } else { "Yellow" })
        }
        $global:LASTEXITCODE = 0
        
    } finally {
        Pop-Location
    }
}
