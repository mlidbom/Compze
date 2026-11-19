# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Set-PluggableComponents {
    <#
    .SYNOPSIS
    Configures the TestUsingPluggableComponentCombinations file with selected pluggable components

    .DESCRIPTION
    Sets the TestUsingPluggableComponentCombinations file, which says which combinations of pluggable components the
    tests run with, one per line: SqlLayer:DIContainer:Serializer:Transport, such as
    SqliteMemory:Microsoft:Newtonsoft:AspNetCore.

    The combinations start from the defaults file's. The SQL layers and DI containers you select replace those of each
    default combination, and what you don't select stays as the defaults have it: with defaults of
    SqliteMemory:Microsoft:Newtonsoft:AspNetCore and SqliteMemory:Microsoft:Newtonsoft:NamedPipes, -MySql gives
    MySql:Microsoft:Newtonsoft:AspNetCore and MySql:Microsoft:Newtonsoft:NamedPipes.

    Each test project's build copies the file into its output folder, which is where the tests read it. So run the tests
    with a build after changing it, as C-Test does by default, not with --no-build.

    .PARAMETER MicrosoftSqlServer
    Include the Microsoft SQL Server persistence layer (MsSql)

    .PARAMETER MySql
    Include the MySQL persistence layer

    .PARAMETER PostgreSql
    Include the PostgreSQL persistence layer (PgSql)

    .PARAMETER Sqlite
    Include the SQLite (file-based) persistence layer

    .PARAMETER SqliteMemory
    Include the SQLite in-memory persistence layer

    .PARAMETER AllSqlLayers
    Include all SQL persistence layers. Mutually exclusive with individual SQL layer switches.

    .PARAMETER Microsoft
    Include the Microsoft DI container

    .PARAMETER Autofac
    Include the Autofac DI container

    .PARAMETER DryIoc
    Include the DryIoc DI container

    .PARAMETER LightInject
    Include the LightInject DI container

    .PARAMETER AllContainers
    Include all DI containers. Mutually exclusive with individual container switches.

    .PARAMETER AllPermutations
    Include all SQL layers and all DI containers (equivalent to -AllSqlLayers -AllContainers)

    .PARAMETER SetAsDefaults
    Save the configuration as the default too: to both TestUsingPluggableComponentCombinations and
    TestUsingPluggableComponentCombinations.defaults. When no switches are given, the defaults file is used.

    .PARAMETER EnsureValid
    Ensures that the configuration files exist, without changing them. Creates missing files from the defaults or the
    example. Incompatible with all other switches. Useful for ensuring a valid state before builds.

    .EXAMPLE
    C-Set-PluggableComponents
    Configures the tests with the saved defaults (or creates the defaults from the .example file if there are none)

    .EXAMPLE
    C-Set-PluggableComponents -EnsureValid
    Ensures the configuration and defaults files exist, creating them if needed. Changes nothing if they exist.

    .EXAMPLE
    C-Set-PluggableComponents -MySql
    Configures the tests to run with MySQL, keeping the defaults' containers, serializers and transports

    .EXAMPLE
    C-Set-PluggableComponents -SqliteMemory -Microsoft
    Configures the tests to run only with SqliteMemory and the Microsoft DI container, keeping the defaults' transports

    .EXAMPLE
    C-Set-PluggableComponents -AllSqlLayers
    Configures the tests to run with all SQL layers and the rest of each default combination

    .EXAMPLE
    C-Set-PluggableComponents -MicrosoftSqlServer -MySql -AllContainers
    Configures the tests to run with MsSql and MySql against every DI container

    .EXAMPLE
    C-Set-PluggableComponents -AllPermutations
    Configures the tests to run with every SQL layer and every DI container (shorthand for -AllSqlLayers -AllContainers)

    .EXAMPLE
    C-Set-PluggableComponents -SqliteMemory -Microsoft -SetAsDefaults
    Sets SqliteMemory with the Microsoft container as the default configuration for later calls with no switches
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
        [switch]$Autofac,
        [switch]$DryIoc,
        [switch]$LightInject,
        [switch]$AllContainers,

        # Convenience switch for all combinations
        [switch]$AllPermutations,

        # Save as defaults switch
        [switch]$SetAsDefaults,

        # Ensure valid configuration exists switch
        [switch]$EnsureValid
    )

    # The names the tests read, Compze.Hosting.Testing's SqlLayer and DIContainer enums, for each switch.
    $sqlLayerNames = [ordered]@{ MicrosoftSqlServer = 'MsSql'; MySql = 'MySql'; PostgreSql = 'PgSql'; Sqlite = 'Sqlite'; SqliteMemory = 'SqliteMemory' }
    $containerNames = [ordered]@{ Microsoft = 'Microsoft'; Autofac = 'Autofac'; DryIoc = 'DryIoc'; LightInject = 'LightInject' }

    $testConfigPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations"
    $testConfigDefaultsPath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations.defaults"
    $testConfigExamplePath = Join-Path $script:CompzeRoot "src\TestUsingPluggableComponentCombinations.example"

    # The helpers come first: PowerShell knows a function only once its definition has run.

    # Copies the file with the time of the copy as its last write time. Copy-Item keeps the source's, and a build copies
    # the configuration into the test projects' output folders only when it is newer than their copies.
    function CopyAsWrittenNow([string]$source, [string]$destination) {
        Copy-Item -Path $source -Destination $destination -Force
        (Get-Item $destination).LastWriteTime = Get-Date
    }

    function ActiveCombinationsIn([string]$path) {
        @(Get-Content $path | Where-Object { $_ -match '^\s*[^#\s]' } | ForEach-Object { $_.Trim() })
    }

    $selectedSqlLayers = @($sqlLayerNames.Keys | Where-Object { (Get-Variable -Name $_ -ValueOnly).IsPresent } | ForEach-Object { $sqlLayerNames[$_] })
    $selectedContainers = @($containerNames.Keys | Where-Object { (Get-Variable -Name $_ -ValueOnly).IsPresent } | ForEach-Object { $containerNames[$_] })

    # Handle -EnsureValid (must be first and exclusive)
    if ($EnsureValid) {
        if ($selectedSqlLayers.Count -gt 0 -or $selectedContainers.Count -gt 0 -or $AllSqlLayers -or $AllContainers -or $AllPermutations -or $SetAsDefaults) {
            Write-Error "-EnsureValid is incompatible with all other switches"
            return
        }

        if (-not (Test-Path $testConfigDefaultsPath)) {
            if (-not (Test-Path $testConfigExamplePath)) {
                Write-Error "Cannot create defaults file: example file not found at $testConfigExamplePath"
                return
            }
            CopyAsWrittenNow $testConfigExamplePath $testConfigDefaultsPath
        }

        if (-not (Test-Path $testConfigPath)) { CopyAsWrittenNow $testConfigDefaultsPath $testConfigPath }
        return
    }

    if ($AllPermutations) {
        $AllSqlLayers = [switch]$true
        $AllContainers = [switch]$true
    }

    if ($AllSqlLayers -and $selectedSqlLayers.Count -gt 0) {
        Write-Error "Cannot specify both -AllSqlLayers and individual SQL layer switches"
        return
    }
    if ($AllContainers -and $selectedContainers.Count -gt 0) {
        Write-Error "Cannot specify both -AllContainers and individual container switches"
        return
    }
    if ($AllSqlLayers) { $selectedSqlLayers = @($sqlLayerNames.Values) }
    if ($AllContainers) { $selectedContainers = @($containerNames.Values) }

    if (-not (Test-Path $testConfigDefaultsPath)) {
        if (-not (Test-Path $testConfigExamplePath)) {
            Write-Error "No defaults file and no example file found at $testConfigExamplePath"
            return
        }
        Write-Host "No defaults file found. Creating from example file..." -ForegroundColor Yellow
        CopyAsWrittenNow $testConfigExamplePath $testConfigDefaultsPath
    }

    # No switches: the defaults as they are.
    if ($selectedSqlLayers.Count -eq 0 -and $selectedContainers.Count -eq 0) {
        if ($PSCmdlet.ShouldProcess($testConfigPath, "Apply default pluggable component combinations")) {
            CopyAsWrittenNow $testConfigDefaultsPath $testConfigPath
            foreach ($combination in (ActiveCombinationsIn $testConfigPath)) { Write-Host "  $combination" -ForegroundColor Cyan }
        }
        return
    }

    $defaultCombinations = ActiveCombinationsIn $testConfigDefaultsPath
    if ($defaultCombinations.Count -eq 0) {
        Write-Error "The defaults file, $testConfigDefaultsPath, has no combination that isn't commented out"
        return
    }

    $combinations = [System.Collections.Generic.List[string]]::new()
    foreach ($defaultCombination in $defaultCombinations) {
        $parts = $defaultCombination -split ':'
        if ($parts.Count -ne 4) {
            Write-Error "The defaults file's combination '$defaultCombination' isn't SqlLayer:DIContainer:Serializer:Transport"
            return
        }
        $sqlLayers = if ($selectedSqlLayers.Count -gt 0) { $selectedSqlLayers } else { @($parts[0]) }
        $containers = if ($selectedContainers.Count -gt 0) { $selectedContainers } else { @($parts[1]) }
        foreach ($sqlLayer in $sqlLayers) {
            foreach ($container in $containers) {
                $combination = "${sqlLayer}:${container}:$($parts[2]):$($parts[3])"
                if (-not $combinations.Contains($combination)) { $combinations.Add($combination) }
            }
        }
    }

    $header = @"
#When running tests, all tests that uses dependency injection and persistence will
#be executed once for every configured combination of components in this file.
#Format is SqlLayer:DIContainer:Serializer:Transport. Comment out the ones you do not want with #.
#Empty/Whitespace lines are ignored

"@
    $content = $header + ($combinations -join "`n") + "`n"

    if ($PSCmdlet.ShouldProcess($testConfigPath, "Update pluggable component combinations")) {
        Set-Content -Path $testConfigPath -Value $content -NoNewline

        if ($SetAsDefaults) {
            Set-Content -Path $testConfigDefaultsPath -Value $content -NoNewline
            Write-Host "Saved as defaults to:" -ForegroundColor Green
            Write-Host "  $testConfigDefaultsPath" -ForegroundColor Cyan
        }

        Write-Host "Updated $testConfigPath with $($combinations.Count) combination(s):" -ForegroundColor Green
        foreach ($combination in $combinations) { Write-Host "  $combination" -ForegroundColor Cyan }
    }
}
