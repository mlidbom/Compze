# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-StartLabEndpoint {
    <#
    .SYNOPSIS
    Starts the lab endpoint in the lab machine's guest, and returns its address

    .DESCRIPTION
    Copies the built lab program (test/Compze.Tests.AcrossMachines.Lab, Debug) into the guest and starts it there, serving
    the lab endpoint over TCP on the guest's address on the network it shares with the host. Returns the endpoint's
    address, which the lab program's converse command on the host takes:

        test\Compze.Tests.AcrossMachines.Lab\bin\Debug\net10.0\Compze.Tests.AcrossMachines.Lab.exe converse <address>

    A lab endpoint already running in the guest is stopped first. The program runs in the guest on its own, not as part
    of this command, so it goes on serving until it is stopped, the guest is, or the machine is started from its baseline
    again. What it writes to its console is in the guest, in C:\CompzeLab\lab-endpoint.log:

        Invoke-InLabGuest { Get-Content C:\CompzeLab\lab-endpoint.log }

    The guest must be up (C-VM-StartLabMachineFromBaseline) and the lab program built (C-Build).

    .EXAMPLE
    C-VM-StartLabEndpoint
    compze.tcp://192.168.39.230:49712/
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()

    $ErrorActionPreference = 'Stop'
    $labProgram = 'Compze.Tests.AcrossMachines.Lab'
    $builtLabProgram = Join-Path $script:CompzeRoot "test\$labProgram\bin\Debug\net10.0"
    if (-not (Test-Path (Join-Path $builtLabProgram "$labProgram.exe"))) {
        Write-CommandFailure -Command $PSCmdlet -Message "The lab program is not built: $labProgram.exe is missing from $builtLabProgram. Run C-Build first."
        return
    }

    $guestLabFolder = 'C:\CompzeLab'
    $guestLabProgram = "$guestLabFolder\$labProgram"

    Invoke-InLabGuest -ArgumentList $labProgram -ScriptBlock {
        param($labProgram)
        $deadline = (Get-Date).AddSeconds(20)
        while ($running = @(Get-Process $labProgram -ErrorAction SilentlyContinue)) {
            if ((Get-Date) -gt $deadline) { throw "The lab program already running in the guest would not stop." }
            # One that exits on its own before it is stopped makes Stop-Process fail; one that won't stop is reported above.
            $running | Stop-Process -Force -ErrorAction SilentlyContinue
            Start-Sleep -Milliseconds 200
        }
    }
    Copy-FolderIntoLabGuest -Folder $builtLabProgram -GuestFolder $guestLabProgram

    $labEndpointAddress = Invoke-InLabGuest -ArgumentList $guestLabFolder, $guestLabProgram, $labProgram, (Get-LabGuestAddress) -ScriptBlock {
        param($labFolder, $labProgramFolder, $labProgram, $listenOn)
        $ErrorActionPreference = 'Stop'
        $addressFile = Join-Path $labFolder 'lab-endpoint-address.txt'
        $log = Join-Path $labFolder 'lab-endpoint.log'
        if (Test-Path $addressFile) { [IO.File]::Delete($addressFile) }

        # Started through WMI, the program is no child of this PowerShell Direct session, so it goes on running when the
        # session ends. cmd.exe is there to write the program's console output to the log.
        $commandLine = "cmd.exe /c `"`"$labProgramFolder\$labProgram.exe`" serve $listenOn `"$addressFile`" > `"$log`" 2>&1`""
        $started = Invoke-CimMethod -ClassName Win32_Process -MethodName Create -Arguments @{ CommandLine = $commandLine; CurrentDirectory = $labProgramFolder }
        if ($started.ReturnValue -ne 0) { throw "Starting the lab program in the guest failed: Win32_Process.Create returned $($started.ReturnValue)." }

        # The first start in a guest that has just booted is slow: it took 71 s where every later start took a second or
        # two (2026-11-13), the guest still busy with its own startup and its antivirus meeting the program for the first time.
        $deadline = (Get-Date).AddMinutes(4)
        while ($true) {
            $address = if (Test-Path $addressFile) { Get-Content $addressFile -Raw -ErrorAction SilentlyContinue }
            if ($address) { return $address.Trim() }
            $exited = -not (Get-Process -Id $started.ProcessId -ErrorAction SilentlyContinue)
            if ($exited -or (Get-Date) -gt $deadline) {
                $why = if ($exited) { 'the lab program exited' } else { 'it had not written its address after 4 minutes' }
                throw "The lab endpoint did not start listening: $why. Its output:`n$(Get-Content $log -Raw -ErrorAction SilentlyContinue)"
            }
            Start-Sleep -Milliseconds 200
        }
    }

    $global:LASTEXITCODE = 0
    $labEndpointAddress
}
