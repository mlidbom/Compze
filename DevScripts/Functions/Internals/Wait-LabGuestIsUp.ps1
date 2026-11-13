function Wait-LabGuestIsUp {
    <#
    .SYNOPSIS
    Waits until the lab machine's guest is up: it answers over PowerShell Direct and has its address on the network it shares with the host

    .DESCRIPTION
    Neither holds while the guest boots. PowerShell Direct throws until Windows is far enough along, and the guest gets its
    IPv4 address from the virtual switch some seconds after that. Every error is taken for "not up yet" and tried again
    each second, since what a booting guest throws varies; at the timeout the last error is thrown, so a guest that refuses
    for good, such as on a wrong credential, says why.

    .PARAMETER Timeout
    How long to wait before throwing.
    #>
    param(
        [Parameter(Mandatory)]
        [timespan]$Timeout
    )

    $deadline = (Get-Date) + $Timeout
    while ($true) {
        try {
            if (Invoke-InLabGuest { [bool](Get-NetIPAddress -AddressFamily IPv4 -PrefixOrigin Dhcp -ErrorAction SilentlyContinue) }) { return }
            $whyNotUp = 'it answers over PowerShell Direct, but has no IPv4 address from the virtual switch yet'
        } catch {
            $whyNotUp = $_.Exception.Message
        }
        if ((Get-Date) -gt $deadline) {
            throw "The guest of '$((Get-LabMachine).Name)' was not up within $([int]$Timeout.TotalSeconds) s: $whyNotUp"
        }
        Start-Sleep -Seconds 1
    }
}
