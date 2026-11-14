function Get-LabHostAddress {
    <#
    .SYNOPSIS
    The IPv4 address the host has on the network it shares with the lab machine's guest

    .DESCRIPTION
    The address the guest connects to for what the host serves, such as the lab's registry server. It is the host's
    address on the lab machine's virtual switch, and like the guest's it changes: the Default Switch takes a new subnet
    whenever the host restarts. So read it for each run, never keep it.
    #>
    (Get-NetIPAddress -InterfaceAlias "vEthernet ($((Get-LabMachine).Switch))" -AddressFamily IPv4).IPAddress
}
