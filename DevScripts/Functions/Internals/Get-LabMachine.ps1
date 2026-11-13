function Get-LabMachine {
    <#
    .SYNOPSIS
    The lab machine: the Hyper-V virtual machine Compze's endpoints run in when they are tested across machines

    .DESCRIPTION
    Returns what the C-VM-* commands agree on about the lab machine, in one place so they cannot disagree:
    its name in Hyper-V, where its files are, where the credential of its guest's account is, the checkpoint every run
    starts from, the virtual switch that connects it to the host, and the computer name of the Windows in it (its guest).

    C-VM-CreateLabMachine creates it. The lab's folder is outside the repository: the machine's disk is tens of gigabytes,
    and the credential is no one's to commit.
    #>
    $labFolder = 'C:\CompzeVMLab'
    $name = 'Compze-Win11-24H2'
    [pscustomobject]@{
        Name                = $name
        LabFolder           = $labFolder
        MachineFolder       = Join-Path $labFolder "virtual-machines\$name"
        GuestCredentialPath = Join-Path $labFolder 'guest-credential.xml'
        BaselineCheckpoint  = 'baseline'
        Switch              = 'Default Switch'
        GuestComputerName   = 'COMPZE-W11-24H2'
    }
}
