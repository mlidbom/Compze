# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-PluggableComponents {
    <#
    .SYNOPSIS
    Displays the currently active pluggable component combinations
    
    .DESCRIPTION
    Reads and displays the active pluggable component combinations from the
    TestUsingPluggableComponentCombinations file. Shows which SQL layers and
    DI containers are configured for testing.
    
    .EXAMPLE
    C-Get-PluggableComponents
    Displays the currently active component combinations
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    $combinations = @(Get-ActiveCombinations)
    if ($combinations.Count -eq 0) {
        Write-Warning "No active component combinations found in src/TestUsingPluggableComponentCombinations"
        return
    }

    foreach ($combination in $combinations) {
        Write-Host "  $combination" -ForegroundColor Cyan
    }
}
