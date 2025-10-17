# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function GitBisect-FindFirstGoodCommit {
    <#
    .SYNOPSIS
    Searches backwards through history to find a good commit for git bisect
    
    .DESCRIPTION
    Internal helper function that searches backwards through git history
    to find a commit where the build succeeds and tests pass.
    Marks the commit as good in git bisect when found.
    
    .PARAMETER FailureText
    Text that indicates a test failure
    
    .PARAMETER MaxFailures
    Maximum cumulative test failures allowed
    
    .PARAMETER Iterations
    Number of times to run the test suite for each commit
    
    .PARAMETER GoodSearchSteps
    Number of commits to go back when searching
    
    .OUTPUTS
    Boolean - $true if good commit found, $false if reached beginning of history
    
    .EXAMPLE
    GitBisect-FindFirstGoodCommit -FailureText "NullRef" -Iterations 5 -GoodSearchSteps 10
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$FailureText,
        [int]$MaxFailures,
        [int]$Iterations,
        [int]$GoodSearchSteps
    )
    
    Write-Host "`nSearching for a good commit..." -ForegroundColor Cyan
    $stepsBack = $GoodSearchSteps
    $foundGood = $false
    
    while (-not $foundGood) {
        # Get the commit hash that is $stepsBack commits back, including all merged branches
        $targetCommit = git log --all --oneline --format=%H --skip=$stepsBack -n 1 2>$null
        
        if (-not $targetCommit -or $LASTEXITCODE -ne 0) {
            Write-Error "Could not go back $stepsBack commits. Reached beginning of history."
            git bisect reset
            return $false
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
        C-Test-Commit -NoBuild -FailureText $FailureText -MaxFailures $MaxFailures -Iterations $Iterations
        $isGood = $LASTEXITCODE -eq 0
        
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
    
    return $true
}
