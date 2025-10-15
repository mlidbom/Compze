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
        # Perform clean and build once before all iterations
        if ($FullGitReset) {
            if ($WhatIfPreference) {
                C-Clean -FullGitReset -WhatIf
                return $true
            } else {
                C-Clean -FullGitReset
            }
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return $false
            }
        }
        elseif ($Clean) {
            C-Clean
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return $false
            }
        }
        elseif (-not $NoBuild) {
            dotnet build $solutionPath
            
            if ($LASTEXITCODE -ne 0) {
                Write-Error "Build failed!"
                return $false
            }
        }
        
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n--- Test iteration $i of $Iterations ---" -ForegroundColor Cyan
            }
            
            # Run tests and capture output
            $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
            $testOutput = dotnet test $solutionPath --no-build 2>&1
            $stopwatch.Stop()
            
            # Display output, filtering out VSTest noise
            $testOutput | C-Filter-TestOutput | ForEach-Object { Write-Host $_ }
            
            # Parse test results to count failures
            $iterationFailures = 0
            $summaryLines = $testOutput | Where-Object { $_ -match '(Passed!|Failed!)\s+-\s+Failed:' }
            
            if ($summaryLines) {
                foreach ($line in $summaryLines) {
                    $summaryText = $line.ToString()
                    
                    if ($summaryText -match 'Failed:\s*(\d+)') {
                        $iterationFailures += [int]$matches[1]
                    }
                }
            }
            
            $totalFailures += $iterationFailures
            
            # Display iteration summary
            $elapsedSeconds = [math]::Round($stopwatch.Elapsed.TotalSeconds, 1)
            if ($Iterations -gt 1) {
                Write-Host "Iteration $i failures: $iterationFailures (cumulative: $totalFailures) elapsed: $elapsedSeconds seconds" -ForegroundColor $(if ($iterationFailures -gt 0) { "Yellow" } else { "Green" })
            } else {
                Write-Host "Failures: $iterationFailures elapsed: $elapsedSeconds seconds" -ForegroundColor $(if ($iterationFailures -gt 0) { "Yellow" } else { "Green" })
            }
            
            # Check for FailureText if specified
            if ($FailureText) {
                $outputString = $testOutput | Out-String
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
