function C-Set-PluggableComponents {
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
    
    .PARAMETER EnsureValid
    Ensures that required configuration files exist without making changes. Creates missing files from defaults/example.
    Incompatible with all other switches. Useful for ensuring valid state before builds.
    
    .EXAMPLE
    C-Set-PluggableComponents
    Configures tests using saved defaults (or creates defaults from .example file if none exist)
    
    .EXAMPLE
    C-Set-PluggableComponents -EnsureValid
    Ensures configuration and defaults files exist, creating them if needed. Makes no changes if files exist.
    
    .EXAMPLE
    C-Set-PluggableComponents -SqliteMemory -Microsoft
    Configures tests to run only with SqliteMemory and Microsoft DI container
    
    .EXAMPLE
    C-Set-PluggableComponents -AllSqlLayers -Microsoft
    Configures tests to run with all SQL layers but only Microsoft DI container
    
    .EXAMPLE
    C-Set-PluggableComponents -MicrosoftSqlServer -MySql -AllContainers
    Configures tests to run with MicrosoftSqlServer and MySql against both DI containers
    
    .EXAMPLE
    C-Set-PluggableComponents -AllSqlLayers -AllContainers
    Configures tests to run with all possible combinations
    
    .EXAMPLE
    C-Set-PluggableComponents -AllPermutations
    Configures tests to run with all possible combinations (shorthand for -AllSqlLayers -AllContainers)
    
    .EXAMPLE
    C-Set-PluggableComponents -SqliteMemory -Microsoft -SetAsDefaults
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
        [switch]$SetAsDefaults,
        
        # Ensure valid configuration exists switch
        [switch]$EnsureValid
    )
    
    $testConfigPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations"
    $testConfigDefaultsPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations.defaults"
    $testConfigExamplePath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations.example"
    
    # Handle -EnsureValid (must be first and exclusive)
    if ($EnsureValid) {
        # Check if any other switches are specified
        $sqlLayerSwitches = @($MicrosoftSqlServer, $MySql, $PostgreSql, $Sqlite, $SqliteMemory, $AllSqlLayers)
        $containerSwitches = @($Microsoft, $SimpleInjector, $AllContainers)
        $otherSwitches = @($AllPermutations, $SetAsDefaults)
        
        if (($sqlLayerSwitches -contains $true) -or ($containerSwitches -contains $true) -or ($otherSwitches -contains $true)) {
            Write-Error "-EnsureValid is incompatible with all other switches"
            return
        }
        
        $filesCreated = @()
        
        # Ensure defaults file exists (create from example if needed)
        if (-not (Test-Path $testConfigDefaultsPath)) {
            if (Test-Path $testConfigExamplePath) {
                Copy-Item -Path $testConfigExamplePath -Destination $testConfigDefaultsPath -Force
                $filesCreated += "TestUsingPluggableComponentCombinations.defaults (from .example)"
            } else {
                Write-Error "Cannot create defaults file: example file not found at $testConfigExamplePath"
                return
            }
        }
        
        # Ensure active config file exists (create from defaults)
        if (-not (Test-Path $testConfigPath)) {
            if (Test-Path $testConfigDefaultsPath) {
                Copy-Item -Path $testConfigDefaultsPath -Destination $testConfigPath -Force
                $filesCreated += "TestUsingPluggableComponentCombinations (from .defaults)"
            } else {
                Write-Error "Cannot create config file: defaults file not found"
                return
            }
        }
        
        # Only output on errors - silent success
        
        return
    }
    
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
