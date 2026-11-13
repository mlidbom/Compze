function Get-LabGuestAddress {
    <#
    .SYNOPSIS
    The IPv4 address the lab machine's guest has on the network it shares with the host

    .DESCRIPTION
    The address an endpoint in the guest listens on, and the host connects to. The virtual switch hands it out as the
    guest boots, and it changes: the Default Switch takes a new subnet whenever the host restarts. So read it for each
    run, never keep it.
    #>
    $addresses = @(Invoke-InLabGuest { Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp | ForEach-Object IPAddress })
    if ($addresses.Count -ne 1) {
        throw "Expected the guest of '$((Get-LabMachine).Name)' to have exactly one IPv4 address from the virtual switch. It has $($addresses.Count): $($addresses -join ', ')"
    }
    $addresses[0]
}
