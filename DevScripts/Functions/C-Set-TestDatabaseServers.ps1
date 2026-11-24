# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Set-TestDatabaseServers {
    <#
    .SYNOPSIS
    Chooses the machine whose SQL Server, PostgreSQL and MySQL the tests use: this one, or another on the network

    .DESCRIPTION
    The tests get their databases from the DbPool, which reads the server for each of the three server persistence layers
    from a user environment variable:

        COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING
        COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING
        COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING

    With -Computer, all three name that machine, with the development credentials CI uses as well: sa for SQL Server,
    postgres and root, each with the password Development!1. A name is resolved to its IPv4 address, which is what the
    other machine's PostgreSQL lets in. Each server's port is tried first, then a login with each connection string, and
    nothing is changed unless all three let this machine in. The logins are made by LogInToTestDatabaseServers.cs, beside
    this command, which dotnet run builds the first time.

    With -ThisMachine, the three variables are removed, so the DbPool falls back to the servers on this machine.

    The variables are set for the user and for this PowerShell session. Processes already running keep what they had:
    restart Rider, and with it NCrunch, and other terminals, for them to follow.

    What the other machine needs: each server reachable from this one (TCP/IP enabled on SQL Server, mixed-mode
    authentication and sa enabled; a pg_hba.conf line for this machine's subnet; a MySQL root@'%'), and its firewall
    letting in 1433, 5432 and 3306.

    Without either switch, it shows which servers the tests use now.

    .PARAMETER Computer
    The machine whose servers the tests are to use, by name or IPv4 address, such as threadripper

    .PARAMETER ThisMachine
    The tests are to use this machine's servers

    .EXAMPLE
    C-Set-TestDatabaseServers -Computer threadripper

    .EXAMPLE
    C-Set-TestDatabaseServers -ThisMachine

    .EXAMPLE
    C-Set-TestDatabaseServers
    Shows which servers the tests use now
    #>
    [CmdletBinding(DefaultParameterSetName = 'Show')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseShouldProcessForStateChangingFunctions', '', Justification = 'A development setting that the same command switches back')]
    param(
        [Parameter(ParameterSetName = 'Computer', Mandatory)]
        [string]$Computer,

        [Parameter(ParameterSetName = 'ThisMachine', Mandatory)]
        [switch]$ThisMachine
    )

    $servers = @(
        @{ Variable = 'COMPOSABLE_MSSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING'; Port = 1433; ConnectionStringFor = { param($address) "Data Source=$address;Initial Catalog=master;User Id=sa;Password=Development!1;TrustServerCertificate=True;" } }
        @{ Variable = 'COMPOSABLE_PGSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING'; Port = 5432; ConnectionStringFor = { param($address) "Host=$address;Database=postgres;Username=postgres;Password=Development!1;" } }
        @{ Variable = 'COMPOSABLE_MYSQL_DATABASE_POOL_MASTER_CONNECTIONSTRING'; Port = 3306; ConnectionStringFor = { param($address) "Server=$address;Database=mysql;Uid=root;Pwd=Development!1;" } }
    )

    switch ($PSCmdlet.ParameterSetName) {
        'Show' {
            foreach ($server in $servers) {
                $connectionString = [Environment]::GetEnvironmentVariable($server.Variable, 'User')
                $shown = if ($null -eq $connectionString) { '(not set: this machine)' }
                         elseif ($connectionString -eq '') { '(empty: this machine)' }
                         else { $connectionString }
                Write-Host ("{0}: {1}" -f $server.Variable, $shown)
            }
        }
        'ThisMachine' {
            foreach ($server in $servers) {
                [Environment]::SetEnvironmentVariable($server.Variable, $null, 'User')
                Set-Item -Path "Env:$($server.Variable)" -Value $null
            }
        }
        'Computer' {
            $address = Resolve-IPv4AddressOf $Computer
            $unreachable = $servers | Where-Object { -not (Test-TcpPortAnswers -Address $address -Port $_.Port) } | ForEach-Object { $_.Port }
            if ($unreachable) {
                throw "$Computer ($address) doesn't answer on port(s) $($unreachable -join ', '). Nothing was changed."
            }
            $connectionStrings = $servers | ForEach-Object { & $_.ConnectionStringFor $address }
            $refusals = dotnet run --file (Join-Path $PSScriptRoot 'Internals/LogInToTestDatabaseServers.cs') -- @connectionStrings
            if ($LASTEXITCODE -ne 0) {
                throw "$Computer ($address) doesn't let this machine log in. Nothing was changed.`n$($refusals -join "`n")"
            }
            foreach ($server in $servers) {
                $connectionString = & $server.ConnectionStringFor $address
                [Environment]::SetEnvironmentVariable($server.Variable, $connectionString, 'User')
                Set-Item -Path "Env:$($server.Variable)" -Value $connectionString
            }
        }
    }
}

function Resolve-IPv4AddressOf([string]$computer) {
    $address = [Net.Dns]::GetHostAddresses($computer) | Where-Object AddressFamily -eq 'InterNetwork' | Select-Object -First 1
    if (-not $address) { throw "$computer has no IPv4 address." }
    $address.IPAddressToString
}

function Test-TcpPortAnswers([string]$Address, [int]$Port) {
    $client = [Net.Sockets.TcpClient]::new()
    try { $client.ConnectAsync($Address, $Port).Wait(3000) }
    catch [AggregateException] { $false }
    finally { $client.Dispose() }
}
