function C-GitBisectAuto {
    <#
    .SYNOPSIS
    Automatically runs git bisect to find the commit that introduced a test failure
    
    .DESCRIPTION
    Automates git bisect to find the commit that introduced a test failure.
    The function will:
    1. Start git bisect and mark current commit as bad
    2. Search backwards for a good commit (builds and tests pass)
    3. Automatically test each commit during bisect
    4. Mark commits as good/bad/skip based on build and test results
    
    .PARAMETER FailureText
    Text that indicates a test failure. If this text appears in test output, the commit is marked as bad.
    Case-insensitive substring match. Mutually exclusive with -MaxFailures.
    
    .PARAMETER MaxFailures
    Maximum cumulative test failures allowed across all iterations before marking commit as bad.
    Mutually exclusive with -FailureText.
    
    .PARAMETER Iterations
    Number of times to run the test suite for each commit (default: 1).
    Useful for intermittent failures.
    
    .PARAMETER GoodSearchSteps
    Number of commits to go back when searching for a good commit (default: 10)
    
    .EXAMPLE
    C-GitBisectAuto -FailureText "System.NullReferenceException" -Iterations 5 -GoodSearchSteps 20
    Runs bisect looking for commits where "System.NullReferenceException" appears in test output,
    testing each commit 5 times, starting the search 20 commits back
    
    .EXAMPLE
    C-GitBisectAuto -MaxFailures 10 -Iterations 10 -GoodSearchSteps 15
    Runs bisect looking for commits where cumulative failures across 10 test runs exceed 10,
    starting the search 15 commits back
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$FailureText,
        
        [int]$MaxFailures = -1,
        
        [int]$Iterations = 1,
        
        [int]$GoodSearchSteps = 10
    )
    
    # Validate mutually exclusive parameters
    if ($FailureText -and $MaxFailures -ge 0) {
        Write-Error "Parameters -FailureText and -MaxFailures are mutually exclusive. Use one or the other."
        return
    }
    
    if (-not $FailureText -and $MaxFailures -lt 0) {
        Write-Error "You must specify either -FailureText or -MaxFailures"
        return
    }
    
    Push-Location $script:CompzeRoot
    try {
        # Save current commit for reference
        $originalCommit = git rev-parse HEAD
        Write-Host "Starting bisect from commit: $originalCommit" -ForegroundColor Cyan
        
        # Start git bisect
        Write-Host "`nStarting git bisect..." -ForegroundColor Cyan
        git bisect start
        
        # Mark current commit as bad
        Write-Host "Marking current commit as bad..." -ForegroundColor Yellow
        git bisect bad
        
        # Find a good commit by searching backwards
        Write-Host "`nSearching for a good commit..." -ForegroundColor Cyan
        $stepsBack = $GoodSearchSteps
        $foundGood = $false
        
        while (-not $foundGood) {
            # Get the commit hash that is $stepsBack commits back, including all merged branches
            $targetCommit = git log --all --oneline --format=%H --skip=$stepsBack -n 1 2>$null
            
            if (-not $targetCommit -or $LASTEXITCODE -ne 0) {
                Write-Error "Could not go back $stepsBack commits. Reached beginning of history."
                git bisect reset
                return
            }
            
            Write-Host "`nChecking commit $stepsBack steps back ($targetCommit)..." -ForegroundColor Cyan
            git checkout $targetCommit 2>&1 | Out-Null
            
            # Try to build
            Write-Host "Building..." -ForegroundColor Yellow
            C-Build -Clean
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Build failed, going back $GoodSearchSteps more commits..." -ForegroundColor Red
                $stepsBack += $GoodSearchSteps
                continue
            }
            
            # Test the commit
            $isGood = Test-Commit -FailureText $FailureText -MaxFailures $MaxFailures -Iterations $Iterations
            
            if (-not $isGood) {
                Write-Host "Tests failed, going back $GoodSearchSteps more commits..." -ForegroundColor Red
                $stepsBack += $GoodSearchSteps
                continue
            }
            
            # Found a good commit
            Write-Host "Found good commit: $targetCommit" -ForegroundColor Green
            # Reset any changes from build before marking as good
            git reset --hard HEAD 2>&1 | Out-Null
            git bisect good
            $foundGood = $true
        }
        
        # Now bisect will automatically checkout commits to test
        Write-Host "`nStarting automated bisect process..." -ForegroundColor Cyan
        
        while ($true) {
            # Get current commit
            $currentCommit = git rev-parse HEAD
            
            # Check if bisect is done (git will tell us)
            $statusCheck = git status 2>&1 | Out-String
            if ($statusCheck -notmatch "You are currently bisecting") {
                Write-Host "`nBisect complete!" -ForegroundColor Green
                Write-Host "The first bad commit is:" -ForegroundColor Green
                git bisect log | Select-Object -First 10 | ForEach-Object { Write-Host $_ }
                Write-Host "`nRepository left in bisect state. Use 'git bisect reset' to return to original state." -ForegroundColor Yellow
                return
            }
            
            Write-Host "`n=== Testing commit: $currentCommit ===" -ForegroundColor Cyan
            
            # Try to build
            Write-Host "Building..." -ForegroundColor Yellow
            C-Build -Clean
            
            if ($LASTEXITCODE -ne 0) {
                Write-Host "Build failed, marking as skip..." -ForegroundColor Yellow
                # Reset any changes from build before skipping
                git reset --hard HEAD 2>&1 | Out-Null
                git bisect skip
                continue
            }
            
            # Test the commit
            $isGood = Test-Commit -FailureText $FailureText -MaxFailures $MaxFailures -Iterations $Iterations
            
            # Reset any changes from build/test before marking
            git reset --hard HEAD 2>&1 | Out-Null
            
            if ($isGood) {
                Write-Host "Tests passed, marking as good..." -ForegroundColor Green
                git bisect good
            } else {
                Write-Host "Tests failed, marking as bad..." -ForegroundColor Red
                git bisect bad
            }
        }
        
    } finally {
        Pop-Location
    }
}

function Test-Commit {
    param(
        [string]$FailureText,
        [int]$MaxFailures,
        [int]$Iterations
    )
    
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
            
            # Display output
            $testOutput | ForEach-Object { Write-Host $_ }
            
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
