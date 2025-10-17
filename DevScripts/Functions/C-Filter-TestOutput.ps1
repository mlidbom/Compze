# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Filter-TestOutput {
    <#
    .SYNOPSIS
    Filters out VSTest noise from test output
    
    .DESCRIPTION
    Helper function to filter out unwanted VSTest messages from test output,
    making test results more readable. This includes version information,
    startup messages, and skipped test notifications.
    
    .PARAMETER InputObject
    The test output line to filter
    
    .EXAMPLE
    dotnet test | C-Filter-TestOutput | ForEach-Object { Write-Host $_ }
    
    .EXAMPLE
    $testOutput | C-Filter-TestOutput | ForEach-Object { Write-Host $_ }
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(ValueFromPipeline = $true)]
        $InputObject
    )
    
    process {
        $line = $InputObject.ToString()
        $shouldDisplay = -not [string]::IsNullOrWhiteSpace($line) -and
            $line -notmatch '^VSTest version' -and
            $line -notmatch '^Starting test execution, please wait\.\.\.' -and
            $line -notmatch '^A total of \d+ test files matched the specified pattern\.' -and
            $line -notmatch '^Test run for .+\.dll \(\.NETCoreApp,Version=' -and
            $line -notmatch '^\s+Skipped .+\[\d+\s+\w+\]' -and
            $line -notmatch '^\[xUnit\.net \d+:\d+:\d+\.\d+\]\s+.+\[SKIP\]'
        
        if ($shouldDisplay) {
            $InputObject
        }
    }
}
