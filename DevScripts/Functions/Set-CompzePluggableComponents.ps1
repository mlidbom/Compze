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
    
    .PARAMETER SetAsDefaults
    Save the current configuration as the default. The configuration will be saved to both
    TestUsingPluggableComponentCombinations and TestUsingPluggableComponentCombinations.defaults.
    When no switches are provided, the defaults file will be used.
    
    .EXAMPLE
    Set-CompzePluggableComponents
    Configures tests using saved defaults (or creates defaults from .example file if none exist)
    
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
    
    .EXAMPLE
    Set-CompzePluggableComponents -SqliteMemory -Microsoft -SetAsDefaults
    Sets SqliteMemory + Microsoft as the default configuration for future calls with no parameters
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
        [switch]$AllPermutations,
        
        # Save as defaults switch
        [switch]$SetAsDefaults
    )
    
    $testConfigPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations"
    $testConfigDefaultsPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations.defaults"
    $testConfigExamplePath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations.example"
    
    # Handle -AllPermutations shorthand
    if ($AllPermutations) {
        $AllSqlLayers = $true
        $AllContainers = $true
    }
    
    # Check if any parameters were specified
    $sqlLayerSwitches = @($MicrosoftSqlServer, $MySql, $PostgreSql, $Sqlite, $SqliteMemory)
    $containerSwitches = @($Microsoft, $SimpleInjector)
    $anySqlLayerSpecified = $sqlLayerSwitches -contains $true
    $anyContainerSpecified = $containerSwitches -contains $true
    $anyParameterSpecified = $AllSqlLayers -or $AllContainers -or $AllPermutations -or $anySqlLayerSpecified -or $anyContainerSpecified
    
    # If no parameters specified, use defaults file
    if (-not $anyParameterSpecified) {
        # Ensure defaults file exists
        if (-not (Test-Path $testConfigDefaultsPath)) {
            if (Test-Path $testConfigExamplePath) {
                Write-Host "No defaults file found. Creating from example file..." -ForegroundColor Yellow
                Copy-Item -Path $testConfigExamplePath -Destination $testConfigDefaultsPath -Force
            } else {
                Write-Error "No defaults file and no example file found at $testConfigExamplePath"
                return
            }
        }
        
        # Copy defaults to active config
        if ($PSCmdlet.ShouldProcess($testConfigPath, "Apply default pluggable component combinations")) {
            Copy-Item -Path $testConfigDefaultsPath -Destination $testConfigPath -Force
            Write-Host "Applied default pluggable component combinations from:" -ForegroundColor Green
            Write-Host "  $testConfigDefaultsPath" -ForegroundColor Cyan
        }
        return
    }
    
    # Validate mutually exclusive options for SQL layers
    if ($AllSqlLayers -and $anySqlLayerSpecified) {
        Write-Error "Cannot specify both -AllSqlLayers and individual SQL layer switches"
        return
    }
    
    # Validate mutually exclusive options for containers
    if ($AllContainers -and $anyContainerSpecified) {
        Write-Error "Cannot specify both -AllContainers and individual container switches"
        return
    }
    
    # Validate that at least something is selected when parameters are provided
    if (-not $AllSqlLayers -and -not $anySqlLayerSpecified) {
        Write-Error "Must specify at least one SQL layer (or -AllSqlLayers)"
        return
    }
    
    if (-not $AllContainers -and -not $anyContainerSpecified) {
        Write-Error "Must specify at least one DI container (or -AllContainers)"
        return
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
    
    # Write to the configuration file(s)
    if ($PSCmdlet.ShouldProcess($testConfigPath, "Update pluggable component combinations")) {
        Set-Content -Path $testConfigPath -Value $content -NoNewline
        
        if ($SetAsDefaults) {
            Set-Content -Path $testConfigDefaultsPath -Value $content -NoNewline
            Write-Host "Saved as defaults to:" -ForegroundColor Green
            Write-Host "  $testConfigDefaultsPath" -ForegroundColor Cyan
        }
        
        Write-Host "Updated $testConfigPath with $($combinations.Count) combination(s):" -ForegroundColor Green
        foreach ($combination in $combinations) {
            Write-Host "  $combination" -ForegroundColor Cyan
        }
    }
}
