function Get-ConfiguredHyperVHost {
    <#
    .SYNOPSIS
    The other machine whose Hyper-V the lab machine runs on, or nothing when it runs on this machine's

    .DESCRIPTION
    The lab machine runs on this machine's Hyper-V unless the lab's folder holds a hyper-v-host.json that names another
    machine, such as a workstation nobody works at, so that the lab machine takes its memory from that machine and not
    from the one development happens on:

        { "ComputerName": "192.168.0.11", "CredentialFile": "C:\\Users\\<user>\\AppData\\Local\\DeskmancerDevelopment\\threadripper-credential.xml" }

    - ComputerName: what reaches the host, by WinRM, so it must be in this machine's WinRM TrustedHosts, and by its C$
      share.
    - CredentialFile: a PSCredential of an administrator of the host, saved with Export-Clixml, so DPAPI-encrypted for
      this machine's user.

    The host has the lab's folder at the same path as this machine, with a lab machine of its own in it, and the C-VM-*
    commands run there (Invoke-LabCommandOnHyperVHost). A lab machine on this machine is left as it is: -OnThisMachine
    uses it. Deskmancer names its Hyper-V host in a file of the same name and fields, in its own lab folder.
    #>
    $configurationFile = Join-Path (Get-LabMachine).LabFolder 'hyper-v-host.json'
    if (-not (Test-Path $configurationFile)) { return $null }

    $configuration = Get-Content -Raw -LiteralPath $configurationFile | ConvertFrom-Json
    $hyperVHost = [pscustomobject]@{
        ComputerName = $configuration.ComputerName
        Credential   = Import-Clixml $configuration.CredentialFile
    }
    if (-not (Test-Path (Get-PathThroughTheHyperVHostShare -HyperVHost $hyperVHost -PathOnTheHost 'C:\'))) {
        throw "The Hyper-V host $($hyperVHost.ComputerName) that $configurationFile names can't be reached through its C`$ share."
    }
    $hyperVHost
}
