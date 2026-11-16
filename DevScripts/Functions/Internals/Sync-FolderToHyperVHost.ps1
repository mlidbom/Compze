function Sync-FolderToHyperVHost {
    <#
    .SYNOPSIS
    Makes a folder on the Hyper-V host match a folder on this machine, copying only what differs

    .DESCRIPTION
    The files in the folder that -FileFilter selects are copied when they differ, and the host's other files there are
    deleted. The subfolders named in -ExcludedFolders are left alone on both sides. robocopy does it, over the host's C$
    share.

    .PARAMETER HyperVHost
    The host, as Get-ConfiguredHyperVHost returns it.

    .PARAMETER Folder
    The folder on this machine.

    .PARAMETER PathOnTheHost
    The folder on the host's C: that is made to match it.

    .PARAMETER FileFilter
    The files to copy, as robocopy takes them, such as '*.ps1'.

    .PARAMETER ExcludedFolders
    The names of subfolders to leave alone, such as 'bin'.
    #>
    param(
        [Parameter(Mandatory)]
        $HyperVHost,

        [Parameter(Mandatory)]
        [string]$Folder,

        [Parameter(Mandatory)]
        [string]$PathOnTheHost,

        [string[]]$FileFilter = @('*.*'),

        [string[]]$ExcludedFolders = @()
    )

    $destination = Get-PathThroughTheHyperVHostShare -HyperVHost $HyperVHost -PathOnTheHost $PathOnTheHost
    $exclusion = if ($ExcludedFolders) { @('/XD') + $ExcludedFolders } else { @() }
    & robocopy $Folder $destination @FileFilter /MIR /XJ /R:2 /W:2 /NP /NJH /NJS /NFL /NDL @exclusion | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "robocopy could not make $destination match $Folder (exit $LASTEXITCODE)." }
    $global:LASTEXITCODE = 0 # robocopy's 1 to 7 mean that it copied or deleted files, and must not read as a failure
}
