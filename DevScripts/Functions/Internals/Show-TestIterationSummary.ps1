function Show-TestIterationSummary {
    <#
    .SYNOPSIS
    Displays a summary of a test iteration
    
    .DESCRIPTION
    Helper function that displays test iteration results with appropriate formatting and colors.
    Handles both single and multiple iteration display formats.
    
    .PARAMETER IterationNumber
    The current iteration number
    
    .PARAMETER TotalIterations
    The total number of iterations
    
    .PARAMETER Failed
    Number of failed tests in this iteration
    
    .PARAMETER Passed
    Number of passed tests in this iteration
    
    .PARAMETER Skipped
    Number of skipped tests in this iteration
    
    .PARAMETER Total
    Total number of tests executed in this iteration
    
    .PARAMETER CumulativeFailures
    Cumulative failures across all iterations so far
    
    .PARAMETER ElapsedSeconds
    Time taken for this iteration in seconds
    
    .EXAMPLE
    Show-TestIterationSummary -IterationNumber 1 -TotalIterations 5 -Failed 0 -Passed 100 -Skipped 2 -Total 102 -CumulativeFailures 0 -ElapsedSeconds 4.5
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$IterationNumber,
        
        [Parameter(Mandatory = $true)]
        [int]$TotalIterations,
        
        [Parameter(Mandatory = $true)]
        [int]$Failed,
        
        [Parameter(Mandatory = $true)]
        [int]$Passed,
        
        [Parameter(Mandatory = $true)]
        [int]$Skipped,
        
        [Parameter(Mandatory = $true)]
        [int]$Total,
        
        [Parameter(Mandatory = $true)]
        [int]$CumulativeFailures,
        
        [Parameter(Mandatory = $true)]
        [decimal]$ElapsedSeconds
    )
    
    $color = if ($Failed -gt 0) { "Yellow" } else { "Green" }
    
    if ($TotalIterations -gt 1) {
        Write-Host "Iteration $IterationNumber - Failed: $Failed, Passed: $Passed, Skipped: $Skipped, Total: $Total, Elapsed: $ElapsedSeconds seconds (cumulative failures: $CumulativeFailures)" -ForegroundColor $color
    } else {
        Write-Host "Failed: $Failed, Passed: $Passed, Skipped: $Skipped, Total: $Total, Elapsed: $ElapsedSeconds seconds" -ForegroundColor $color
    }
}
