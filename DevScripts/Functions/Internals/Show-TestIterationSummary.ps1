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
    
    .PARAMETER Failures
    Number of failures in this iteration
    
    .PARAMETER CumulativeFailures
    Cumulative failures across all iterations so far
    
    .PARAMETER ElapsedSeconds
    Time taken for this iteration in seconds
    
    .EXAMPLE
    Show-TestIterationSummary -IterationNumber 1 -TotalIterations 5 -Failures 0 -CumulativeFailures 0 -ElapsedSeconds 4.5
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [int]$IterationNumber,
        
        [Parameter(Mandatory = $true)]
        [int]$TotalIterations,
        
        [Parameter(Mandatory = $true)]
        [int]$Failures,
        
        [Parameter(Mandatory = $true)]
        [int]$CumulativeFailures,
        
        [Parameter(Mandatory = $true)]
        [decimal]$ElapsedSeconds
    )
    
    $color = if ($Failures -gt 0) { "Yellow" } else { "Green" }
    
    if ($TotalIterations -gt 1) {
        Write-Host "Iteration $IterationNumber failures: $Failures (cumulative: $CumulativeFailures) elapsed: $ElapsedSeconds seconds" -ForegroundColor $color
    } else {
        Write-Host "Failures: $Failures elapsed: $ElapsedSeconds seconds" -ForegroundColor $color
    }
}
