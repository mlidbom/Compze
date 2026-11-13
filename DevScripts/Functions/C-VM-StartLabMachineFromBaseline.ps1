# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-StartLabMachineFromBaseline {
    <#
    .SYNOPSIS
    Starts the lab machine from its baseline checkpoint, and waits until its guest is up

    .DESCRIPTION
    Restores the 'baseline' checkpoint C-VM-CreateLabMachine took, of the prepared guest shut down, boots it, and waits
    until the guest answers over PowerShell Direct and has its address on the network it shares with the host. A lab
    machine that runs is turned off first, and whatever was in its guest is discarded: every run starts from the same guest.

    .EXAMPLE
    C-VM-StartLabMachineFromBaseline
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine

    if ((Get-VM -Name $machine.Name).State -ne 'Off') { Stop-VM -Name $machine.Name -TurnOff -Force }
    Restore-VMSnapshot -VMName $machine.Name -Name $machine.BaselineCheckpoint -Confirm:$false
    Start-VM -Name $machine.Name
    Wait-LabGuestIsUp -Timeout ([timespan]::FromMinutes(5))
    $global:LASTEXITCODE = 0
}
