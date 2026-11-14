# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-TestLabNetwork {
    <#
    .SYNOPSIS
    Tries the network between the host and the lab machine's guest, and the guest's way to the internet, and writes how each went

    .DESCRIPTION
    Writes one line for each thing it tried: whether it worked, and how fast, or how it failed.

      The host connects to the guest    Over TCP, to a port the guest listens at for this. A conversation with the lab
                                        endpoint needs it. The guest's firewall lets it in through the rule
                                        C-VM-CreateLabMachine adds.
      The guest connects to the host    Over TCP, to a port the host listens at for this. A registry server on the host
                                        needs it. The host's firewall lets it in through a rule of the host's own,
                                        'Hyper-V guests on the Default Switch may reach this computer', which names the
                                        virtual switch's network adapter. Without that rule the connect gets no answer.
      The guest pings the host          The same rule of the host's firewall lets it in.
      The host downloads the file       From the internet, for the guest's download to be compared with.
      The guest downloads the file      The same file, right after. Where the guest fails and the host succeeds, the
                                        guest has lost its way out through the virtual switch. Where both fail, the
                                        internet, or the file's site, is what cannot be reached.

    The guest must be up (C-VM-StartLabMachineFromBaseline). Nothing is changed on either side, and nothing is kept of
    what is downloaded.

    .PARAMETER FileToDownload
    The address of the file both sides download.

    .EXAMPLE
    C-VM-TestLabNetwork

    Tried                          Worked How
    -----                          ------ ---
    The host connects to the guest   True connected in 2 ms
    The guest connects to the host   True connected in 15 ms
    The guest pings the host         True answered
    The host downloads the file      True 76676 bytes in 0.7 s
    The guest downloads the file     True 76676 bytes in 0.6 s
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [string]$FileToDownload = 'https://dot.net/v1/dotnet-install.ps1'
    )

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine
    $guestAddress = Get-LabGuestAddress
    $hostAddress = (Get-NetIPAddress -InterfaceAlias "vEthernet ($($machine.Switch))" -AddressFamily IPv4).IPAddress

    function Write-WhatWasTried([string]$tried, $outcome) { [pscustomobject]@{ Tried = $tried; Worked = [bool]$outcome.Worked; How = [string]$outcome.How } }

    # The two script blocks below run on whichever side connects or downloads: here, and in the guest's Windows PowerShell.
    $connect = {
        param($address, $port)
        $watch = [Diagnostics.Stopwatch]::StartNew()
        $client = [Net.Sockets.TcpClient]::new()
        try {
            if ($client.ConnectAsync($address, $port).Wait(5000)) { [pscustomobject]@{ Worked = $true; How = "connected in $($watch.ElapsedMilliseconds) ms" } }
            else { [pscustomobject]@{ Worked = $false; How = 'no answer in 5 s' } }
        } catch {
            [pscustomobject]@{ Worked = $false; How = $_.Exception.GetBaseException().Message }
        } finally {
            $client.Dispose()
        }
    }

    # curl is part of Windows. It writes the status, the size, the time and its own error message, bars between them.
    $download = {
        param($file)
        $status, $bytes, $seconds, $curlsError = (& curl.exe --silent --location --output NUL --max-time 30 --write-out '%{http_code}|%{size_download}|%{time_total}|%{errormsg}' $file) -split '\|', 4
        $took = "$([math]::Round([double]::Parse($seconds, [Globalization.CultureInfo]::InvariantCulture), 1)) s"
        if ($LASTEXITCODE -ne 0) { [pscustomobject]@{ Worked = $false; How = "failed after ${took}: $curlsError" } }
        elseif ($status -ne '200') { [pscustomobject]@{ Worked = $false; How = "the site answered $status after $took" } }
        else { [pscustomobject]@{ Worked = $true; How = "$bytes bytes in $took" } }
    }

    # The guest listens at a port its Windows picks, says which, and waits for the host's connect. It runs as a job, so
    # that the host can connect while the guest waits.
    function Test-HostConnectsToGuest {
        $guestListening = Invoke-Command -VMName $machine.Name -Credential (Import-Clixml $machine.GuestCredentialPath) -AsJob -ArgumentList $guestAddress -ScriptBlock {
            param($address)
            $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Parse($address), 0)
            $listener.Start()
            try {
                $listener.LocalEndpoint.Port
                [void]$listener.AcceptTcpClientAsync().Wait(15000)
            } finally {
                $listener.Stop()
            }
        }
        try {
            $deadline = (Get-Date).AddSeconds(30)
            while (-not ($port = Receive-Job $guestListening)) {
                if ((Get-Date) -gt $deadline) { throw 'The guest did not start listening within 30 s.' }
                Start-Sleep -Milliseconds 100
            }
            & $connect $guestAddress $port
        } finally {
            Remove-Job $guestListening -Force
        }
    }

    function Test-GuestConnectsToHost {
        $listener = [Net.Sockets.TcpListener]::new([Net.IPAddress]::Parse($hostAddress), 0)
        $listener.Start()
        try {
            Invoke-InLabGuest -ScriptBlock $connect -ArgumentList $hostAddress, $listener.LocalEndpoint.Port
        } finally {
            $listener.Stop()
        }
    }

    function Test-GuestPingsHost {
        $answered = Invoke-InLabGuest -ArgumentList $hostAddress -ScriptBlock { param($address) Test-Connection $address -Count 1 -Quiet }
        [pscustomobject]@{ Worked = $answered; How = if ($answered) { 'answered' } else { 'no answer' } }
    }

    Write-WhatWasTried 'The host connects to the guest' (Test-HostConnectsToGuest)
    Write-WhatWasTried 'The guest connects to the host' (Test-GuestConnectsToHost)
    Write-WhatWasTried 'The guest pings the host' (Test-GuestPingsHost)
    Write-WhatWasTried 'The host downloads the file' (& $download $FileToDownload)
    Write-WhatWasTried 'The guest downloads the file' (Invoke-InLabGuest -ScriptBlock $download -ArgumentList $FileToDownload)
    $global:LASTEXITCODE = 0
}
