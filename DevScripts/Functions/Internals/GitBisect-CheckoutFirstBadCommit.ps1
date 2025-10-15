function GitBisect-CheckoutFirstBadCommit {
    <#
    .SYNOPSIS
    Resets git bisect state and checks out the first bad commit
    
    .DESCRIPTION
    Internal helper function that completes a git bisect operation.
    Parses the bisect log to find the first bad commit, resets bisect state,
    and checks out the first bad commit so you can investigate it.
    
    .OUTPUTS
    None
    
    .EXAMPLE
    GitBisect-CheckoutFirstBadCommit
    #>
    [CmdletBinding()]
    param()
    
    Write-Host "`nBisect complete!" -ForegroundColor Green
    Write-Host "The first bad commit is:" -ForegroundColor Green
    
    # Get the first bad commit from bisect log
    $bisectLog = git bisect log
    $firstBadLine = $bisectLog | Where-Object { $_ -match '^# bad: \[([a-f0-9]+)\]' } | Select-Object -First 1
    
    if ($firstBadLine -match '^# bad: \[([a-f0-9]+)\]') {
        $firstBadCommit = $matches[1]
        
        # Display the bisect log
        $bisectLog | Select-Object -First 10 | ForEach-Object { Write-Host $_ }
        
        # Reset bisect state first
        Write-Host "`nResetting bisect state and checking out first bad commit..." -ForegroundColor Cyan
        git bisect reset 2>&1 | Out-Null
        
        # Check out the first bad commit
        git checkout $firstBadCommit 2>&1 | Out-Null
        
        Write-Host "Repository is now at the first bad commit: $firstBadCommit" -ForegroundColor Green
        Write-Host "Use 'git checkout <branch>' to return to your original branch." -ForegroundColor Yellow
    } else {
        # Fallback if we can't parse the log
        $bisectLog | Select-Object -First 10 | ForEach-Object { Write-Host $_ }
        Write-Host "`nCouldn't determine first bad commit. Repository left in bisect state." -ForegroundColor Yellow
        Write-Host "Use 'git bisect reset' to return to original state." -ForegroundColor Yellow
    }
}
