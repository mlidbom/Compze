# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function GitBisect-FindFirstBadCommit {
    <#
    .SYNOPSIS
    Runs the automated git bisect loop to find the first bad commit
    
    .DESCRIPTION
    Internal helper function that runs the main git bisect loop.
    Tests each commit that git bisect checks out, marking them as good/bad/skip.
    Continues until git bisect identifies the first bad commit.
    
    .PARAMETER FailureText
    Text that indicates a test failure
    
    .PARAMETER MaxFailures
    Maximum cumulative test failures allowed
    
    .PARAMETER Iterations
    Number of times to run the test suite for each commit
    
    .OUTPUTS
    None - Function continues until bisect completes
    
    .EXAMPLE
    GitBisect-FindFirstBadCommit -FailureText "NullRef" -Iterations 5
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$FailureText,
        [int]$MaxFailures,
        [int]$Iterations
    )
    
    Write-Host "`nStarting automated bisect process..." -ForegroundColor Cyan
    
    $lastTestedCommit = $null
    
    while ($true) {
        # Get current commit
        $currentCommit = git rev-parse HEAD
        
        # Check if we're testing the same commit again - means bisect is done
        if ($lastTestedCommit -eq $currentCommit) {
            return
        }
        
        $lastTestedCommit = $currentCommit
        
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
        C-Test-Commit -NoBuild -FailureText $FailureText -MaxFailures $MaxFailures -Iterations $Iterations
        $isGood = $LASTEXITCODE -eq 0
        
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
}
