# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
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
    
    # Group tests by class name and sort
    $testsByClass = @{}
    foreach ($test in $uniqueFailedTests) {
        $fullName = $test.FullName
        if ($fullName -match '^(.+)\.([^.]+)$') {
            $className = $matches[1]
            $methodName = $matches[2]
            
            if (-not $testsByClass.ContainsKey($className)) {
                $testsByClass[$className] = @()
            }
            $testsByClass[$className] += $methodName
        } else {
            # Fallback if pattern doesn't match - treat entire name as class
            if (-not $testsByClass.ContainsKey($fullName)) {
                $testsByClass[$fullName] = @()
            }
        }
    }
    
    # Display grouped and sorted tests
    $sortedClasses = $testsByClass.Keys | Sort-Object
    foreach ($className in $sortedClasses) {
        Write-Host $className -ForegroundColor Red
        $sortedMethods = $testsByClass[$className] | Sort-Object
        foreach ($methodName in $sortedMethods) {
            Write-Host "   $methodName" -ForegroundColor Red
        }
    }
    
    Write-Host "`nTotal unique failed tests: $($uniqueFailedTests.Count)" -ForegroundColor Red
}
