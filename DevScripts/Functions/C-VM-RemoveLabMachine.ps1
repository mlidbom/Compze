# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-RemoveLabMachine {
    <#
    .SYNOPSIS
    Removes the lab machine: the virtual machine, its checkpoints and its disk

    .DESCRIPTION
    Turns the lab machine off if it runs, removes it from Hyper-V and deletes its folder. Whatever is in its guest is
    lost; C-VM-CreateLabMachine makes a new one. The lab's folder and the guest credential in it stay.

    Asks before removing; pass -Confirm:$false to remove without asking.

    .PARAMETER OnThisMachine
    Remove the lab machine on this machine's Hyper-V, even when the lab's folder names a Hyper-V host to run it on
    (Get-ConfiguredHyperVHost).

    .EXAMPLE
    C-VM-RemoveLabMachine -Confirm:$false
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$OnThisMachine
    )

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine
    $hyperVHost = if (-not $OnThisMachine) { Get-ConfiguredHyperVHost }
    $whichMachine = if ($hyperVHost) { "$($machine.Name) on $($hyperVHost.ComputerName)" } else { $machine.Name }
    if (-not $PSCmdlet.ShouldProcess($whichMachine, 'Remove the virtual machine, its checkpoints and its disk')) { return }
    if ($hyperVHost) { return Invoke-LabCommandOnHyperVHost -HyperVHost $hyperVHost -Command $PSCmdlet }

    $virtualMachine = Get-VM -Name $machine.Name -ErrorAction SilentlyContinue
    if ($virtualMachine) {
        if ($virtualMachine.State -ne 'Off') { Stop-VM -VM $virtualMachine -TurnOff -Force }
        Remove-VM -VM $virtualMachine -Force
    }
    if (Test-Path $machine.MachineFolder) { Remove-Item $machine.MachineFolder -Recurse -Force }
    $global:LASTEXITCODE = 0
}
