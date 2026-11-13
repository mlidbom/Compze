function Invoke-InLabGuest {
    <#
    .SYNOPSIS
    Runs a script block in the lab machine's guest and returns what it returns

    .DESCRIPTION
    Runs it over PowerShell Direct: through Hyper-V's own channel into the guest, which needs no network, as the guest's
    account, elevated, in the guest's Windows PowerShell. An error in the guest stops the script block and is thrown here.

    The guest must be up: C-VM-StartLabMachineFromBaseline waits until it is.

    .PARAMETER ScriptBlock
    What to run in the guest. It sees nothing of the host's variables: pass what it needs as -ArgumentList.

    .PARAMETER ArgumentList
    The script block's arguments, bound to its param(...) in order.

    .EXAMPLE
    Invoke-InLabGuest { $env:COMPUTERNAME }
    #>
    param(
        [Parameter(Mandatory, Position = 0)]
        [scriptblock]$ScriptBlock,

        [object[]]$ArgumentList = @()
    )

    $machine = Get-LabMachine
    Invoke-Command -VMName $machine.Name -Credential (Import-Clixml $machine.GuestCredentialPath) -ErrorAction Stop -ArgumentList $ArgumentList -ScriptBlock $ScriptBlock
}
