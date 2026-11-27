# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Watch-DbPool {
    <#
    .SYNOPSIS
    Watches the DbPool's reservations of one SQL layer's databases on this machine while tests run: how full the pool is, who holds what, and for how long

    .DESCRIPTION
    Every pool on the machine that uses the same SQL layer shares one machine-wide state: the pool's databases, and which
    pool, in which process, holds each. A test that waits too long for a free database fails with "Timed out waiting for
    database". This shows why: once a second, how many databases are reserved, by how many pools, how many databases each
    pool holds, the oldest reservation's age, and the reservations per test project. Each reservation and release goes to
    the log, and at the end a summary: the most reserved at once, how long every database was reserved, how long the
    reservations were held, the longest held, and those still held.

    It reads the state's file without taking its lock, so watching changes nothing in what it watches. It reads the state
    with the Compze.DbPool.dll this checkout built, so build first. Start it, then start the test run in another shell.
    It stops at Ctrl+C, after Duration, or once the pool has held no database for StopAfterIdle.

    The work is done by Internals/WatchDbPool.cs, run with dotnet run --file.

    .PARAMETER SqlLayer
    The SQL layer whose pool to watch. SqliteMemory, the default combinations' layer, if left out.

    .PARAMETER Duration
    How long to watch. Until Ctrl+C, or StopAfterIdle, if left out.

    .PARAMETER StopAfterIdle
    Stops once the pool has held no database for this long, after it has held some. Never, if left out.

    .PARAMETER IntervalMilliseconds
    How often to read the state. 100 milliseconds if left out.

    .PARAMETER LogPath
    Where to write the log. obj\dbpool-watch-<SqlLayer>-<time>.log in the repository if left out.

    .PARAMETER Checkout
    The checkout whose build of Compze.DbPool.dll to read the state with, such as a worktree of an older commit being bisected. This
    module's own checkout if left out. DbPool versions before 0.4.0 don't record the process of a reservation.

    .PARAMETER ReservationLength
    How long a reservation lasts, which dates each reservation from its expiry. The DbPool's 65 seconds if left out.

    .EXAMPLE
    C-Watch-DbPool -StopAfterIdle 00:00:30
    Watches the SqliteMemory pool until it has held no database for 30 seconds: start it, then C-Test in another shell

    .EXAMPLE
    C-Watch-DbPool -SqlLayer MySql -Duration 00:05:00
    Watches the MySQL pool for five minutes
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [ValidateSet('SqliteMemory', 'Sqlite', 'MsSql', 'MySql', 'PgSql')]
        [string]$SqlLayer = 'SqliteMemory',

        [TimeSpan]$Duration = [TimeSpan]::Zero,

        [TimeSpan]$StopAfterIdle = [TimeSpan]::Zero,

        [int]$IntervalMilliseconds = 100,

        [string]$LogPath,

        [string]$Checkout = $script:CompzeRoot,

        [TimeSpan]$ReservationLength = [TimeSpan]::FromSeconds(65)
    )

    $sqlLayerTypeNames = @{
        SqliteMemory = 'Compze.DbPool.Sqlite._private.SqliteMemoryDbPoolSqlLayer'
        Sqlite       = 'Compze.DbPool.Sqlite._private.SqliteDbPoolSqlLayer'
        MsSql        = 'Compze.DbPool.MicrosoftSql._private.MsSqlDbPoolSqlLayer'
        MySql        = 'Compze.DbPool.MySql._private.MySqlDbPoolSqlLayer'
        PgSql        = 'Compze.DbPool.PostgreSql._private.PgSqlDbPoolSqlLayer'
    }

    $dbPoolAssembly = Get-ChildItem (Join-Path $Checkout 'src\Compze.DbPool\bin\Debug\net*\Compze.DbPool.dll') -ErrorAction SilentlyContinue |
        Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $dbPoolAssembly) {
        Write-Error "No Compze.DbPool.dll under $Checkout\src\Compze.DbPool\bin\Debug: build first, with C-Build."
        return
    }

    if (-not $LogPath) {
        $LogPath = Join-Path $script:CompzeRoot "obj\dbpool-watch-$SqlLayer-$(Get-Date -Format 'yyyyMMdd-HHmmss').log"
    }
    New-Item -ItemType Directory -Force (Split-Path -Parent $LogPath) | Out-Null

    $invariant = [Globalization.CultureInfo]::InvariantCulture
    dotnet run --file (Join-Path $PSScriptRoot 'Internals\WatchDbPool.cs') -- `
        $dbPoolAssembly.FullName `
        $sqlLayerTypeNames[$SqlLayer] `
        $ReservationLength.TotalSeconds.ToString($invariant) `
        $IntervalMilliseconds `
        $LogPath `
        ([int]$Duration.TotalSeconds) `
        ([int]$StopAfterIdle.TotalSeconds)
}
