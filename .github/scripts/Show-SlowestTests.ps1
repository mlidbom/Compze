<#
.SYNOPSIS
Lists the slowest test results in the TRX files under a folder: each test's duration, when it started and ended, and its outcome.

.DESCRIPTION
A test that runs slowly and passes says nothing in a job log: dotnet test prints the output only of failed tests. This lists
them, with their start and end times, so that slow tests which overlapped in time can be seen to have stalled together.

.PARAMETER Directory
The folder whose TRX files, in it and below it, are read: those that dotnet test's trx logger writes.

.PARAMETER Top
How many of the slowest results to list.

.EXAMPLE
pwsh .github/scripts/Show-SlowestTests.ps1 -Directory TestResults/run-3 -Top 5
#>
param(
    [Parameter(Mandatory)][string]$Directory,
    [int]$Top = 10
)

$results = Get-ChildItem -Path $Directory -Recurse -Filter *.trx | ForEach-Object {
    ([xml](Get-Content -Raw -LiteralPath $_.FullName)).TestRun.Results.UnitTestResult
} | Where-Object { $_ -and $_.duration }

$results |
    Sort-Object { [TimeSpan]::Parse($_.duration) } -Descending |
    Select-Object -First $Top |
    ForEach-Object {
        '{0,9:N1} s  {1}  {2:HH:mm:ss.fff} to {3:HH:mm:ss.fff}  {4}' -f `
            [TimeSpan]::Parse($_.duration).TotalSeconds, $_.outcome.PadRight(7), `
            ([DateTimeOffset]::Parse($_.startTime)).UtcDateTime, ([DateTimeOffset]::Parse($_.endTime)).UtcDateTime, $_.testName
    }
