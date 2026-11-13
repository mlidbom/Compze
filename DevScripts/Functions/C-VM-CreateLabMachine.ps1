# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-CreateLabMachine {
    <#
    .SYNOPSIS
    Creates the lab machine: the Hyper-V virtual machine Compze's endpoints run in when they are tested across machines

    .DESCRIPTION
    Clones a checkpoint of another virtual machine, whose Windows must sign its account in by itself, and prepares the
    clone's Windows (its guest) for Compze:

      - a computer name of its own, so it and its source can be on a network together
      - no automatic Windows updates, which would load it and restart it in the middle of a run
      - a boot that always reaches Windows: the account's password never expires, and a boot after a power-off goes on
        into Windows instead of into automatic repair
      - a firewall that lets in TCP from the network the guest shares with the host: an endpoint listens at a port
        Windows picks as it starts, so no rule can name the port
      - the host's own .NET runtime, and nothing more of .NET: no ASP.NET Core runtime, so whatever runs in the guest
        proves it needs none

    Then it shuts the guest down and takes the 'baseline' checkpoint, which every run starts from
    (C-VM-StartLabMachineFromBaseline).

    The source machine is read, never changed, and nothing is downloaded. Takes a few minutes. Needs membership of the
    Hyper-V Administrators group.

    .PARAMETER SourceMachine
    The virtual machine to clone.

    .PARAMETER SourceCheckpoint
    The checkpoint of it to clone: one of a guest that was shut down, so the clone boots instead of resuming.

    .PARAMETER SourceGuestCredentialPath
    The credential of the source guest's account, as the user running this exported it with Export-Clixml. The clone's
    guest has the same account, so the file is copied into the lab's folder.

    .PARAMETER DotNetChannel
    The channel of the .NET runtime the guest gets, the one the repository's projects target: the newest runtime of
    that channel installed on the host.

    .EXAMPLE
    C-VM-CreateLabMachine
    Clones the baseline of Deskmancer's Windows 11 24H2 machine
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$SourceMachine = 'Deskmancer-Win11-24H2',
        [string]$SourceCheckpoint = '24H2-baseline',
        [string]$SourceGuestCredentialPath = 'C:\VantageVMLab\guest-credential.xml',
        [string]$DotNetChannel = '10.0'
    )

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine

    if (Get-VM -Name $machine.Name -ErrorAction SilentlyContinue) {
        Write-CommandFailure -Command $PSCmdlet -Message "The lab machine '$($machine.Name)' already exists. C-VM-RemoveLabMachine removes it."
        return
    }

    # Export-VMSnapshot writes the checkpoint as a machine of its own, its disk merged into one file, which Import-VM then
    # copies under a new id. The copy is named after the checkpoint, and its disk after the source.
    function Copy-CheckpointAsTheLabMachine {
        $exportFolder = Join-Path $machine.LabFolder 'export-of-the-source-checkpoint'
        New-Item -ItemType Directory -Path $exportFolder -Force | Out-Null
        try {
            Export-VMSnapshot -VMName $SourceMachine -Name $SourceCheckpoint -Path $exportFolder
            $exportedConfiguration = Get-ChildItem (Join-Path $exportFolder "$SourceMachine\Virtual Machines") -Filter *.vmcx
            $imported = Import-VM -Path $exportedConfiguration.FullName -Copy -GenerateNewId `
                -VirtualMachinePath $machine.MachineFolder -VhdDestinationPath (Join-Path $machine.MachineFolder 'Virtual Hard Disks') `
                -SnapshotFilePath $machine.MachineFolder -SmartPagingFilePath $machine.MachineFolder
        } finally {
            Remove-Item $exportFolder -Recurse -Force
        }
        Rename-VM -VM $imported -NewName $machine.Name
        Set-VM -Name $machine.Name -Notes "Compze's lab machine: its endpoints run here when they are tested across machines. Cloned $(Get-Date -Format 'yyyy-MM-dd') from the checkpoint '$SourceCheckpoint' of '$SourceMachine' by C-VM-CreateLabMachine."

        $disk = Get-VMHardDiskDrive -VMName $machine.Name
        $diskNamedAfterTheMachine = Join-Path (Split-Path $disk.Path -Parent) "$($machine.Name).vhdx"
        Rename-Item -LiteralPath $disk.Path -NewName (Split-Path $diskNamedAfterTheMachine -Leaf)
        Set-VMHardDiskDrive -VMHardDiskDrive $disk -Path $diskNamedAfterTheMachine

        Connect-VMNetworkAdapter -VMName $machine.Name -SwitchName $machine.Switch
    }

    function Initialize-GuestForCompze {
        Invoke-InLabGuest -ArgumentList $machine.GuestComputerName -ScriptBlock {
            param($computerName)
            $ErrorActionPreference = 'Stop'

            Rename-Computer -NewName $computerName -Force -WarningAction SilentlyContinue

            $automaticUpdatesPolicy = 'HKLM:\SOFTWARE\Policies\Microsoft\Windows\WindowsUpdate\AU'
            New-Item -Path $automaticUpdatesPolicy -Force | Out-Null
            Set-ItemProperty -Path $automaticUpdatesPolicy -Name NoAutoUpdate -Value 1 -Type DWord

            Set-LocalUser -Name $env:USERNAME -PasswordNeverExpires $true
            & net accounts /maxpwage:unlimited | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "net accounts /maxpwage:unlimited failed (exit $LASTEXITCODE)." }
            & bcdedit /set '{current}' bootstatuspolicy IgnoreAllFailures | Out-Null
            if ($LASTEXITCODE -ne 0) { throw "bcdedit /set bootstatuspolicy failed (exit $LASTEXITCODE)." }

            New-NetFirewallRule -DisplayName 'Compze lab: TCP from the network shared with the host' -Direction Inbound -Protocol TCP -RemoteAddress LocalSubnet -Action Allow | Out-Null
        }
    }

    # The guest gets the host's own .NET runtime, the one the build's tests ran on, as the files an installation of only
    # the runtime consists of: dotnet.exe, the newest host resolver and the runtime's shared framework. A program's
    # executable finds it in Program Files\dotnet with nothing registered.
    # Nothing is downloaded: the guest's downloads through the Default Switch stalled when this was written (2026-11-13:
    # curl gave up after a minute with "server closed abruptly", and Invoke-WebRequest waited without end).
    function Copy-HostDotNetRuntimeIntoGuest {
        $hostDotNet = Split-Path (Get-Command dotnet).Source -Parent
        $newestFirst = { [version]($_.Name -replace '-.*$') }
        $runtime = Get-ChildItem (Join-Path $hostDotNet 'shared\Microsoft.NETCore.App') -Directory | Where-Object Name -like "$DotNetChannel.*" |
            Sort-Object $newestFirst -Descending | Select-Object -First 1
        if (-not $runtime) { throw "The host has no .NET $DotNetChannel runtime under $hostDotNet\shared\Microsoft.NETCore.App." }
        $hostResolver = Get-ChildItem (Join-Path $hostDotNet 'host\fxr') -Directory | Sort-Object $newestFirst -Descending | Select-Object -First 1

        $runtimeOnlyInstallation = Join-Path ([IO.Path]::GetTempPath()) "Compze\dotnet-runtime-for-the-lab-guest-$([Guid]::NewGuid().ToString('N'))"
        try {
            New-Item -ItemType Directory -Path $runtimeOnlyInstallation -Force | Out-Null
            Copy-Item (Join-Path $hostDotNet 'dotnet.exe') $runtimeOnlyInstallation
            Copy-Item $hostResolver.FullName (Join-Path $runtimeOnlyInstallation "host\fxr\$($hostResolver.Name)") -Recurse
            Copy-Item $runtime.FullName (Join-Path $runtimeOnlyInstallation "shared\Microsoft.NETCore.App\$($runtime.Name)") -Recurse
            Copy-FolderIntoLabGuest -Folder $runtimeOnlyInstallation -GuestFolder 'C:\Program Files\dotnet'
        } finally {
            Remove-Item $runtimeOnlyInstallation -Recurse -Force
        }
    }

    function Restart-GuestCleanly {
        Stop-GuestCleanly
        Start-VM -Name $machine.Name
        Wait-LabGuestIsUp -Timeout ([timespan]::FromMinutes(5))
    }

    # Shut the guest down through Hyper-V's integration services, as from its Start menu, not a power-off.
    function Stop-GuestCleanly {
        Stop-VM -Name $machine.Name -Force
        $state = (Get-VM -Name $machine.Name).State
        if ($state -ne 'Off') { throw "'$($machine.Name)' did not shut down: it is $state." }
    }

    function Assert-GuestIsPrepared {
        $guest = Invoke-InLabGuest {
            [pscustomobject]@{
                ComputerName        = $env:COMPUTERNAME
                DotNetSharedRuntimes = @(Get-ChildItem "$env:ProgramFiles\dotnet\shared" | ForEach-Object { "$($_.Name) $((Get-ChildItem $_.FullName | ForEach-Object Name) -join ' ')" })
            }
        }
        if ($guest.ComputerName -ne $machine.GuestComputerName) {
            throw "The guest's computer name is '$($guest.ComputerName)', not '$($machine.GuestComputerName)'."
        }
        if ($guest.DotNetSharedRuntimes.Count -ne 1 -or $guest.DotNetSharedRuntimes[0] -notlike "Microsoft.NETCore.App $DotNetChannel.*") {
            throw "The guest should have the .NET $DotNetChannel runtime and nothing more of .NET. It has: $($guest.DotNetSharedRuntimes -join '; ')"
        }
    }

    Copy-CheckpointAsTheLabMachine
    Copy-Item $SourceGuestCredentialPath $machine.GuestCredentialPath -Force

    Start-VM -Name $machine.Name
    Wait-LabGuestIsUp -Timeout ([timespan]::FromMinutes(5))
    Initialize-GuestForCompze
    Copy-HostDotNetRuntimeIntoGuest
    Restart-GuestCleanly # The computer name takes effect, and what the baseline holds is a guest that has booted as it will boot from then on.
    Assert-GuestIsPrepared

    Stop-GuestCleanly
    Checkpoint-VM -Name $machine.Name -SnapshotName $machine.BaselineCheckpoint
    $global:LASTEXITCODE = 0
}
