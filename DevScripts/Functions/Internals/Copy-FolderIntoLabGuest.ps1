function Copy-FolderIntoLabGuest {
    <#
    .SYNOPSIS
    Makes a folder in the lab machine's guest a copy of a folder on the host

    .DESCRIPTION
    Zips the host's folder, copies the one file over PowerShell Direct, and expands it in the guest: copying many small
    files one by one over PowerShell Direct is slow. What the guest's folder held before is removed first, so nothing
    an earlier copy left there is taken for this one's.

    .PARAMETER Folder
    The folder on the host.

    .PARAMETER GuestFolder
    The folder in the guest that becomes its copy.
    #>
    param(
        [Parameter(Mandatory)]
        [string]$Folder,

        [Parameter(Mandatory)]
        [string]$GuestFolder
    )

    Add-Type -AssemblyName System.IO.Compression.FileSystem
    $machine = Get-LabMachine
    $zip =Join-Path ([IO.Path]::GetTempPath()) "Compze\folder-for-the-lab-guest-$([Guid]::NewGuid().ToString('N')).zip"
    New-Item -ItemType Directory -Path (Split-Path $zip -Parent) -Force | Out-Null
    $session = New-PSSession -VMName $machine.Name -Credential (Import-Clixml $machine.GuestCredentialPath)
    try {
        [IO.Compression.ZipFile]::CreateFromDirectory($Folder, $zip, [IO.Compression.CompressionLevel]::Fastest, $false)
        $guestZip = Invoke-Command -Session $session -ScriptBlock { Join-Path $env:TEMP "folder-from-the-host-$([Guid]::NewGuid().ToString('N')).zip" }
        Copy-Item -ToSession $session -Path $zip -Destination $guestZip
        Invoke-Command -Session $session -ErrorAction Stop -ArgumentList $guestZip, $GuestFolder -ScriptBlock {
            param($zipFromTheHost, $folder)
            $ErrorActionPreference = 'Stop'
            Add-Type -AssemblyName System.IO.Compression.FileSystem
            if (Test-Path $folder) { [IO.Directory]::Delete($folder, $true) }
            [IO.Compression.ZipFile]::ExtractToDirectory($zipFromTheHost, $folder)
            [IO.File]::Delete($zipFromTheHost)
        }
    } finally {
        Remove-PSSession $session
        if (Test-Path $zip) { Remove-Item $zip -Force }
    }
}
