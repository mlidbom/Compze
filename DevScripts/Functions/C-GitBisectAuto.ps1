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
        if (-not (GitBisect-FindFirstGoodCommit -FailureText $FailureText -MaxFailures $MaxFailures -Iterations $Iterations -GoodSearchSteps $GoodSearchSteps)) {
            return
        }
        
        # Run the automated bisect process to find the first bad commit
        GitBisect-FindFirstBadCommit -FailureText $FailureText -MaxFailures $MaxFailures -Iterations $Iterations
        
    } finally {
        Pop-Location
    }
}
