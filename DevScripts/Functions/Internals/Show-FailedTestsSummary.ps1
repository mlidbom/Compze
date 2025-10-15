function Show-FailedTestsSummary {
    <#
    .SYNOPSIS
    Displays a summary of failed tests
    
    .DESCRIPTION
    Helper function that displays the list of unique failed tests with appropriate formatting.
    Only displays output if there are failed tests to show.
    
    .PARAMETER FailedTests
    Array of failed test objects with FullName properties
    
    .EXAMPLE
    Show-FailedTestsSummary -FailedTests $allFailedTests
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$FailedTests
    )
    
    if ($FailedTests.Count -eq 0) {
        return
    }
    
    Write-Host "`n=== Failed Tests ===" -ForegroundColor Red
    $uniqueFailedTests = $FailedTests | Select-Object -Property FullName -Unique
    foreach ($test in $uniqueFailedTests) {
        Write-Host $test.FullName -ForegroundColor Red
    }
    Write-Host "`nTotal unique failed tests: $($uniqueFailedTests.Count)" -ForegroundColor Red
}
