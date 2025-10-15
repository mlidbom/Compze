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
    
    .PARAMETER FailureText
    Text that indicates a test failure. If this text appears in test output, returns false.
    Case-insensitive substring match. Mutually exclusive with -MaxFailures.
    
    .PARAMETER MaxFailures
    Maximum cumulative test failures allowed across all iterations.
    If cumulative failures exceed this number, returns false.
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
    
    .OUTPUTS
    Boolean - $true if all test runs passed according to the criteria, $false otherwise
    
    .EXAMPLE
    C-Test-Commit -FailureText "System.NullReferenceException" -Iterations 5
    Runs tests 5 times, returns false if "System.NullReferenceException" appears in any run
    
    .EXAMPLE
    C-Test-Commit -MaxFailures 10 -Iterations 10
    Builds then runs tests 10 times, returns false if cumulative failures exceed 10
    
    .EXAMPLE
    C-Test-Commit -NoBuild -MaxFailures 5 -Iterations 3
    Runs tests 3 times without building, returns false if cumulative failures exceed 5
    
    .EXAMPLE
    C-Test-Commit -Clean -MaxFailures 5 -Iterations 3
    Cleans, builds, then runs tests 3 times, returns false if cumulative failures exceed 5
    
    .EXAMPLE
    C-Test-Commit -FullGitReset -MaxFailures 0
    Performs full git clean, builds, then runs tests once, returns false if any failures occur
    
    .EXAMPLE
    C-Test-Commit -FullGitReset -WhatIf
    Shows what would be deleted by git clean without actually deleting anything
    
    .EXAMPLE
    if (C-Test-Commit -MaxFailures 5 -Iterations 3) {
        Write-Host "Commit is good!"
    } else {
        Write-Host "Commit has failures"
    }
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
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
        return $false
    }
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    $totalFailures = 0
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        # Build if needed
        if (-not (C-Build-IfNeeded -NoBuild:$NoBuild -Clean:$Clean -FullGitReset:$FullGitReset)) {
            Write-Error "Build failed!"
            return $false
        }
        
        # Handle -WhatIf for FullGitReset (returns early after showing what would be deleted)
        if ($FullGitReset -and $WhatIfPreference) {
            return $true
        }
        
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n--- Test iteration $i of $Iterations ---" -ForegroundColor Cyan
            }
            
            # Run tests
            $result = C-Run-TestRun -SolutionPath $solutionPath
            
            $totalFailures += $result.Failed
            
            # Display iteration summary
            if ($Iterations -gt 1) {
                Write-Host "Iteration $i failures: $($result.Failed) (cumulative: $totalFailures) elapsed: $($result.ElapsedSeconds) seconds" -ForegroundColor $(if ($result.Failed -gt 0) { "Yellow" } else { "Green" })
            } else {
                Write-Host "Failures: $($result.Failed) elapsed: $($result.ElapsedSeconds) seconds" -ForegroundColor $(if ($result.Failed -gt 0) { "Yellow" } else { "Green" })
            }
            
            # Check for FailureText if specified
            if ($FailureText) {
                $outputString = $result.TestOutput | Out-String
                if ($outputString -match [regex]::Escape($FailureText)) {
                    Write-Host "Found failure text: '$FailureText'" -ForegroundColor Red
                    return $false
                }
            }
            
            # Check for MaxFailures if specified
            if ($MaxFailures -ge 0) {
                if ($totalFailures -gt $MaxFailures) {
                    Write-Host "Cumulative failures ($totalFailures) exceeded MaxFailures ($MaxFailures)" -ForegroundColor Red
                    return $false
                }
            }
        }
        
        # All iterations passed - display final summary
        if ($Iterations -gt 1) {
            Write-Host "Total cumulative failures: $totalFailures" -ForegroundColor $(if ($totalFailures -eq 0) { "Green" } else { "Yellow" })
        }
        return $true
        
    } finally {
        Pop-Location
    }
}
