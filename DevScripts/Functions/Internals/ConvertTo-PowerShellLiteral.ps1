function ConvertTo-PowerShellLiteral {
    <#
    .SYNOPSIS
    A value as PowerShell source that evaluates to it: for the parameter values a C-VM-* command takes

    .DESCRIPTION
    How Invoke-LabCommandOnHyperVHost passes a command's parameters to the Hyper-V host: inside the command it runs
    there, not on a command line, which would go through the quoting of the host's Windows PowerShell and lose the
    double quotes inside a value.

    .PARAMETER Value
    A switch, a bool, an int, a string, or an array of them.
    #>
    param([Parameter(Mandatory)][AllowEmptyString()][AllowEmptyCollection()]$Value)

    if ($Value -is [System.Management.Automation.SwitchParameter] -or $Value -is [bool]) { if ($Value) { '$true' } else { '$false' } }
    elseif ($Value -is [int]) { "$Value" }
    elseif ($Value -is [array]) { '@(' + (@($Value | ForEach-Object { ConvertTo-PowerShellLiteral $_ }) -join ', ') + ')' }
    elseif ($Value -is [string]) { "'" + ($Value -replace "'", "''") + "'" }
    else { throw "No PowerShell literal is made for a $($Value.GetType().FullName), the type of '$Value'." }
}
