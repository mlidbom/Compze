function Get-AbortedTestRunsDescription {
    <#
    .SYNOPSIS
    Describes the test assembly runs that aborted, for the end of C-Test's failure message

    .DESCRIPTION
    An aborted run reports no failed test, so C-Test's count of failed tests says nothing of it. Returns the text to append to
    the failure message: how many test assembly runs aborted, and which assemblies had no test host, such as a project under
    test/ that holds no tests and doesn't set IsTestProject to false. Returns an empty string when none aborted.

    .PARAMETER TestRunResults
    The results of C-Run-TestRun, one for each iteration
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [AllowEmptyCollection()]
        [object[]]$TestRunResults
    )

    $abortedRuns = ($TestRunResults | Measure-Object -Property AbortedRuns -Sum).Sum
    if (-not $abortedRuns) { return '' }

    $withNoTestHost = @($TestRunResults | ForEach-Object { $_.AssembliesWithNoTestHost } | Sort-Object -Unique)
    $description = ", and $abortedRuns test assembly run(s) aborted"
    if ($withNoTestHost.Count -gt 0) {
        $description += ", finding no test host for $($withNoTestHost -join ', ')"
    }
    return $description
}
