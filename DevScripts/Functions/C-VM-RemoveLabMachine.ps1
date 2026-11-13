# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-RemoveLabMachine {
    <#
    .SYNOPSIS
    Removes the lab machine: the virtual machine, its checkpoints and its disk

    .DESCRIPTION
    Turns the lab machine off if it runs, removes it from Hyper-V and deletes its folder. Whatever is in its guest is
    lost; C-VM-CreateLabMachine makes a new one. The lab's folder and the guest credential in it stay.

    Asks before removing; pass -Confirm:$false to remove without asking.

    .EXAMPLE
    C-VM-RemoveLabMachine -Confirm:$false
    #>
    [CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine
    if (-not $PSCmdlet.ShouldProcess($machine.Name, 'Remove the virtual machine, its checkpoints and its disk')) { return }

    $virtualMachine = Get-VM -Name $machine.Name -ErrorAction SilentlyContinue
    if ($virtualMachine) {
        if ($virtualMachine.State -ne 'Off') { Stop-VM -VM $virtualMachine -TurnOff -Force }
        Remove-VM -VM $virtualMachine -Force
    }
    if (Test-Path $machine.MachineFolder) { Remove-Item $machine.MachineFolder -Recurse -Force }
    $global:LASTEXITCODE = 0
}
