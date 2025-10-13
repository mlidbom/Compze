function Set-CompzePluggableComponents {
    <#
    .SYNOPSIS
    Configures the TestUsingPluggableComponentCombinations file with selected pluggable components
    
    .DESCRIPTION
    Sets the TestUsingPluggableComponentCombinations file to contain the cross join of all selected
    SQL persistence layers and DI containers. This controls which component combinations will be tested.
    
    .PARAMETER MicrosoftSqlServer
    Include Microsoft SQL Server persistence layer
    
    .PARAMETER MySql
    Include MySQL persistence layer
    
    .PARAMETER PostgreSql
    Include PostgreSQL persistence layer
    
    .PARAMETER Sqlite
    Include SQLite (file-based) persistence layer
    
    .PARAMETER SqliteMemory
    Include SQLite in-memory persistence layer
    
    .PARAMETER AllSqlLayers
    Include all SQL persistence layers. Mutually exclusive with individual SQL layer switches.
    
    .PARAMETER Microsoft
    Include Microsoft DI container
    
    .PARAMETER SimpleInjector
    Include SimpleInjector DI container
    
    .PARAMETER AllContainers
    Include all DI containers. Mutually exclusive with individual container switches.
    
    .PARAMETER AllPermutations
    Include all SQL layers and all DI containers (equivalent to -AllSqlLayers -AllContainers)
    
    .EXAMPLE
    Set-CompzePluggableComponents
    Configures tests to run with defaults (SqliteMemory and Microsoft DI container)
    
    .EXAMPLE
    Set-CompzePluggableComponents -SqliteMemory -Microsoft
    Configures tests to run only with SqliteMemory and Microsoft DI container
    
    .EXAMPLE
    Set-CompzePluggableComponents -AllSqlLayers -Microsoft
    Configures tests to run with all SQL layers but only Microsoft DI container
    
    .EXAMPLE
    Set-CompzePluggableComponents -MicrosoftSqlServer -MySql -AllContainers
    Configures tests to run with MicrosoftSqlServer and MySql against both DI containers
    
    .EXAMPLE
    Set-CompzePluggableComponents -AllSqlLayers -AllContainers
    Configures tests to run with all possible combinations
    
    .EXAMPLE
    Set-CompzePluggableComponents -AllPermutations
    Configures tests to run with all possible combinations (shorthand for -AllSqlLayers -AllContainers)
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        # SQL Layer switches
        [switch]$MicrosoftSqlServer,
        [switch]$MySql,
        [switch]$PostgreSql,
        [switch]$Sqlite,
        [switch]$SqliteMemory,
        [switch]$AllSqlLayers,
        
        # Container switches
        [switch]$Microsoft,
        [switch]$SimpleInjector,
        [switch]$AllContainers,
        
        # Convenience switch for all combinations
        [switch]$AllPermutations
    )
    
    # Handle -AllPermutations shorthand
    if ($AllPermutations) {
        $AllSqlLayers = $true
        $AllContainers = $true
    }
    
    # Validate mutually exclusive options for SQL layers
    $sqlLayerSwitches = @($MicrosoftSqlServer, $MySql, $PostgreSql, $Sqlite, $SqliteMemory)
    $anySqlLayerSpecified = $sqlLayerSwitches -contains $true
    
    if ($AllSqlLayers -and $anySqlLayerSpecified) {
        Write-Error "Cannot specify both -AllSqlLayers and individual SQL layer switches"
        return
    }
    
    # Validate mutually exclusive options for containers
    $containerSwitches = @($Microsoft, $SimpleInjector)
    $anyContainerSpecified = $containerSwitches -contains $true
    
    if ($AllContainers -and $anyContainerSpecified) {
        Write-Error "Cannot specify both -AllContainers and individual container switches"
        return
    }
    
    # Apply defaults if nothing specified
    if (-not $AllSqlLayers -and -not $anySqlLayerSpecified) {
        $SqliteMemory = $true
        $anySqlLayerSpecified = $true
    }
    
    if (-not $AllContainers -and -not $anyContainerSpecified) {
        $Microsoft = $true
        $anyContainerSpecified = $true
    }
    
    # Build list of SQL layers
    $sqlLayers = @()
    if ($AllSqlLayers) {
        $sqlLayers = @(
            'MicrosoftSqlServer',
            'MySql',
            'PostgreSql',
            'Sqlite',
            'SqliteMemory'
        )
    } else {
        if ($MicrosoftSqlServer) { $sqlLayers += 'MicrosoftSqlServer' }
        if ($MySql) { $sqlLayers += 'MySql' }
        if ($PostgreSql) { $sqlLayers += 'PostgreSql' }
        if ($Sqlite) { $sqlLayers += 'Sqlite' }
        if ($SqliteMemory) { $sqlLayers += 'SqliteMemory' }
    }
    
    # Build list of DI containers
    $containers = @()
    if ($AllContainers) {
        $containers = @(
            'Microsoft',
            'SimpleInjector'
        )
    } else {
        if ($Microsoft) { $containers += 'Microsoft' }
        if ($SimpleInjector) { $containers += 'SimpleInjector' }
    }
    
    # Generate cross join of all combinations
    $combinations = @()
    foreach ($sqlLayer in $sqlLayers) {
        foreach ($container in $containers) {
            $combinations += "${sqlLayer}:${container}"
        }
    }
    
    # Build the configuration file content
    $header = @"
#When running tests, all tests that uses dependency injection and persistence will 
#be executed once for every configured combination of components in this file.
#Format is PersistenceLayer:DIContainer. Comment out the ones you do not want with #. 
#Empty/Whitespace lines are ignored

"@
    
    $content = $header + ($combinations -join "`n") + "`n"
    
    # Write to the configuration file
    $testConfigPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations"
    
    if ($PSCmdlet.ShouldProcess($testConfigPath, "Update pluggable component combinations")) {
        Set-Content -Path $testConfigPath -Value $content -NoNewline
        
        Write-Host "Updated $testConfigPath with $($combinations.Count) combination(s):" -ForegroundColor Green
        foreach ($combination in $combinations) {
            Write-Host "  $combination" -ForegroundColor Cyan
        }
    }
}
