function Write-CommandFailure {
    <#
    .SYNOPSIS
    Reports that a C-* command failed, so that both a PowerShell session and a process that ran the command see it

    .DESCRIPTION
    Sets $global:LASTEXITCODE to 1 and writes $Message as an error of the failing command itself.

    Setting $LASTEXITCODE alone is not enough. When the last statement of `pwsh -Command "...; C-Test"` is a function,
    not a native command, pwsh's exit code is 0 or 1 by $?, and only an error the function writes through its own
    $PSCmdlet makes $? false. Write-Error doesn't, and neither does an error a function it calls writes, so a failed
    build or a failed test run exited 0 and read as a pass to anything checking the exit code, such as an agent.

    The error is non-terminating: an interactive caller sees it and carries on.

    .PARAMETER Command
    The failing command's $PSCmdlet. The command must be an advanced function ([CmdletBinding()]).

    .PARAMETER Message
    What failed, as the user should read it.

    .EXAMPLE
    Write-CommandFailure -Command $PSCmdlet -Message "3 tests failed."
    #>
    param(
        [Parameter(Mandatory)]
        [System.Management.Automation.PSCmdlet]$Command,

        [Parameter(Mandatory)]
        [string]$Message
    )

    $global:LASTEXITCODE = 1
    $Command.WriteError([System.Management.Automation.ErrorRecord]::new([Exception]::new($Message), 'CommandFailed', [System.Management.Automation.ErrorCategory]::NotSpecified, $null))
}
