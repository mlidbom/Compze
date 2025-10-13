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
    
    $testConfigPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations"
    
    if (-not (Test-Path $testConfigPath)) {
        Write-Warning "Configuration file not found: $testConfigPath"
        return
    }
    
    $content = Get-Content -Path $testConfigPath
    
    # Parse combinations (skip comments and empty lines)
    $combinations = $content | Where-Object { 
        $_.Trim() -and 
        -not $_.Trim().StartsWith('#') 
    }
    
    if ($combinations.Count -eq 0) {
        Write-Warning "No active component combinations found in configuration file"
        return
    }
    
    # Parse and display combinations with grouping
    $sqlLayers = @{}
    $containers = @{}
    
    foreach ($combination in $combinations) {
        if ($combination -match '^([^:]+):([^:]+)$') {
            $sqlLayer = $matches[1]
            $container = $matches[2]
            
            if (-not $sqlLayers.ContainsKey($sqlLayer)) {
                $sqlLayers[$sqlLayer] = @()
            }
            $sqlLayers[$sqlLayer] += $container
            
            if (-not $containers.ContainsKey($container)) {
                $containers[$container] = @()
            }
            $containers[$container] += $sqlLayer
            
            Write-Host "  $combination" -ForegroundColor Cyan
        }
    }
}
