function C-Test-Commit {
    <#
    .SYNOPSIS
    Tests a commit by running the test suite multiple times with failure detection
    
    .DESCRIPTION
    Runs the test suite for the current commit with configurable failure detection.
    Useful for testing intermittent failures or as part of automated bisect processes.
    Assumes the solution has already been built (uses --no-build flag).
    
    .PARAMETER FailureText
    Text that indicates a test failure. If this text appears in test output, returns false.
    Case-insensitive substring match. Mutually exclusive with -MaxFailures.
    
    .PARAMETER MaxFailures
    Maximum cumulative test failures allowed across all iterations.
    If cumulative failures exceed this number, returns false.
    Mutually exclusive with -FailureText.
    
    .PARAMETER Iterations
    Number of times to run the test suite (default: 1).
    Useful for intermittent failures.
    
    .OUTPUTS
    Boolean - $true if all test runs passed according to the criteria, $false otherwise
    
    .EXAMPLE
    C-Test-Commit -FailureText "System.NullReferenceException" -Iterations 5
    Runs tests 5 times, returns false if "System.NullReferenceException" appears in any run
    
    .EXAMPLE
    C-Test-Commit -MaxFailures 10 -Iterations 10
    Runs tests 10 times, returns false if cumulative failures exceed 10
    
    .EXAMPLE
    if (C-Test-Commit -MaxFailures 5 -Iterations 3) {
        Write-Host "Commit is good!"
    } else {
        Write-Host "Commit has failures"
    }
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$FailureText,
        
        [int]$MaxFailures = -1,
        
        [int]$Iterations = 1
    )
    
    # Validate mutually exclusive parameters
    if ($FailureText -and $MaxFailures -ge 0) {
        Write-Error "Parameters -FailureText and -MaxFailures are mutually exclusive. Use one or the other."
        return $false
    }
    
    if (-not $FailureText -and $MaxFailures -lt 0) {
        Write-Error "You must specify either -FailureText or -MaxFailures"
        return $false
    }
    
    $solutionPath = Join-Path $script:CompzeRoot "src\Compze.slnx"
    $totalFailures = 0
    
    Push-Location (Join-Path $script:CompzeRoot "src")
    try {
        for ($i = 1; $i -le $Iterations; $i++) {
            if ($Iterations -gt 1) {
                Write-Host "`n--- Test iteration $i of $Iterations ---" -ForegroundColor Cyan
            }
            
            # Run tests and capture output
            $testOutput = dotnet test $solutionPath --no-build 2>&1
            
            # Display output, filtering out VSTest noise
            $testOutput | Where-Object { 
                $line = $_.ToString()
                -not [string]::IsNullOrWhiteSpace($line) -and
                $line -notmatch '^VSTest version' -and
                $line -notmatch '^Starting test execution, please wait\.\.\.' -and
                $line -notmatch '^A total of \d+ test files matched the specified pattern\.' -and
                $line -notmatch '^Test run for .+\.dll \(\.NETCoreApp,Version=' -and
                $line -notmatch '^\s+Skipped .+\[\d+\s+\w+\]' -and
                $line -notmatch '^\[xUnit\.net \d+:\d+:\d+\.\d+\]\s+.+\[SKIP\]'
            } | ForEach-Object { Write-Host $_ }
            
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
                # Parse test results - sum up failures from all test projects
                $summaryLines = $testOutput | Where-Object { $_ -match '(Passed!|Failed!)\s+-\s+Failed:' }
                
                if ($summaryLines) {
                    foreach ($line in $summaryLines) {
                        $summaryText = $line.ToString()
                        
                        if ($summaryText -match 'Failed:\s*(\d+)') {
                            $failures = [int]$matches[1]
                            $totalFailures += $failures
                            
                            if ($Iterations -gt 1) {
                                Write-Host "Iteration $i failures: $failures (cumulative: $totalFailures)" -ForegroundColor Yellow
                            }
                            
                            if ($totalFailures -gt $MaxFailures) {
                                Write-Host "Cumulative failures ($totalFailures) exceeded MaxFailures ($MaxFailures)" -ForegroundColor Red
                                return $false
                            }
                        }
                    }
                }
            }
        }
        
        # All iterations passed
        if ($MaxFailures -ge 0 -and $Iterations -gt 1) {
            Write-Host "Total cumulative failures: $totalFailures (limit: $MaxFailures)" -ForegroundColor Green
        }
        return $true
        
    } finally {
        Pop-Location
    }
}
