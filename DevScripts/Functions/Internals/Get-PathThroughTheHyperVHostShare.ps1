function Get-PathThroughTheHyperVHostShare {
    <#
    .SYNOPSIS
    A path on the Hyper-V host's C: as this machine reaches it: through the host's C$ share

    .PARAMETER HyperVHost
    The host, as Get-ConfiguredHyperVHost returns it.

    .PARAMETER PathOnTheHost
    The path as the host sees it, on its C:.
    #>
    param(
        [Parameter(Mandatory)]
        $HyperVHost,

        [Parameter(Mandatory)]
        [string]$PathOnTheHost
    )

    if ($PathOnTheHost -notlike 'C:\*') { throw "Only the Hyper-V host's C: is reached from here, and $PathOnTheHost isn't on it." }
    Join-Path ('\\{0}\C$' -f $HyperVHost.ComputerName) $PathOnTheHost.Substring(3)
}
