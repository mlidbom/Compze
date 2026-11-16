# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-VM-ConverseWhileTheLabMachineVanishes {
    <#
    .SYNOPSIS
    Holds a conversation with the lab endpoint while the lab machine is made to vanish and to come back, and writes what each tuery experienced

    .DESCRIPTION
    Starts one of the lab program's ways to converse on the host, against the lab endpoint in the lab machine. A few
    seconds in, it makes the machine vanish in the way asked for, and later brings it back. When the conversation is over
    it writes one timeline, in time order: what was done to the machine (the lines marked >>>>>>), what the conversing
    side logged, and one line for each tuery, answered and how fast, or failed, after how long and with what.

    The two ways to converse:

      -LabEndpointAddress  A pure client that is told the lab endpoint's address (the lab program's converse command).
                           The lab endpoint must be running: C-VM-StartLabEndpoint, whose result is the address to pass.
      -ThroughRegistry     An endpoint that is told no address (converse-through-registry). It hosts the lab's registry
                           server on the host, and finds the lab endpoint through the lab registry. This command starts
                           the lab endpoint itself, before the registry server exists, told where the server will listen.

    The ways to vanish:

      Pause              The machine is paused, then resumed. Nothing in it runs meanwhile, and nothing is told.
      DisconnectNetwork  Its network adapter is disconnected from the virtual switch, then connected again. The guest
                         runs on, and keeps its address.
      TurnOff            The machine is turned off, as by a power cut, and stays off: start it from its baseline again.
      KillLabProgram     The lab program is killed in the guest, and stays killed: the machine is up, its endpoint gone.
      RestartLabProgram  The lab program is killed in the guest, then started again: its endpoint comes back at a new
                         address, as an application does that is closed and opened again. Only with -ThroughRegistry:
                         a pure client was told the old address.

    The guest must be up (C-VM-StartLabMachineFromBaseline) and the lab program built (C-Build).

    .PARAMETER LabEndpointAddress
    The lab endpoint's address, as C-VM-StartLabEndpoint returned it.

    .PARAMETER ThroughRegistry
    Converse as an endpoint that finds the lab endpoint through the lab registry.

    .PARAMETER RegistryServerPort
    The port the lab's registry server listens at on the host, with -ThroughRegistry.

    .PARAMETER Vanish
    How the machine vanishes: Pause, DisconnectNetwork, TurnOff, KillLabProgram or RestartLabProgram.

    .PARAMETER ConversationOptions
    Options for the lab program's ways to converse, as it takes them, such as '--tueries-per-round', '8'. Run the lab
    program without arguments to see them. '--for' is set by this command.

    .PARAMETER SecondsBeforeVanishing
    How long the conversation runs before the machine vanishes.

    .PARAMETER SecondsVanished
    How long the machine stays vanished.

    .PARAMETER SecondsAfterComingBack
    How long the conversation goes on after the machine is back.

    .PARAMETER FailuresInFull
    Also write, the first time a tuery fails in each way, the whole exception. Without it a failure is its one line.

    .PARAMETER OnThisMachine
    Use the lab machine on this machine's Hyper-V, even when the lab's folder names a Hyper-V host to run it on
    (Get-ConfiguredHyperVHost). On such a host, the host is the side that converses with the guest.

    .EXAMPLE
    C-VM-ConverseWhileTheLabMachineVanishes -LabEndpointAddress (C-VM-StartLabEndpoint) -Vanish Pause

    .EXAMPLE
    C-VM-ConverseWhileTheLabMachineVanishes -ThroughRegistry -Vanish RestartLabProgram
    The lab endpoint leaves the registry when its program is killed, and is found again at its new address

    .EXAMPLE
    C-VM-ConverseWhileTheLabMachineVanishes -LabEndpointAddress $address -Vanish DisconnectNetwork -ConversationOptions '--tueries-per-round', '8', '--pause-between-rounds', '20000' -SecondsBeforeVanishing 3 -SecondsVanished 14
    Eight connections sit idle in the client's pool while the machine is away, and are needed again once it is back
    #>
    [CmdletBinding(DefaultParameterSetName = 'ToldTheAddress')]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory, ParameterSetName = 'ToldTheAddress')]
        [string]$LabEndpointAddress,

        [Parameter(Mandatory, ParameterSetName = 'ThroughRegistry')]
        [switch]$ThroughRegistry,

        [Parameter(ParameterSetName = 'ThroughRegistry')]
        [int]$RegistryServerPort = 50505,

        [Parameter(Mandatory)]
        [ValidateSet('Pause', 'DisconnectNetwork', 'TurnOff', 'KillLabProgram', 'RestartLabProgram')]
        [string]$Vanish,

        [string[]]$ConversationOptions = @(),
        [int]$SecondsBeforeVanishing = 5,
        [int]$SecondsVanished = 30,
        [int]$SecondsAfterComingBack = 25,
        [switch]$FailuresInFull,
        [switch]$OnThisMachine
    )

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine
    $labProgramName = 'Compze.Tests.AcrossMachines.Lab'
    $builtLabProgram = Get-BuiltOutputFolder -ProjectFile (Join-Path $script:CompzeRoot "test\$labProgramName\$labProgramName.csproj")
    if (-not $builtLabProgram) {
        Write-CommandFailure -Command $PSCmdlet -Message "The lab program ($labProgramName) is not built. Run C-Build first."
        return
    }
    $labProgram = Join-Path $builtLabProgram "$labProgramName.exe"
    if ($Vanish -eq 'RestartLabProgram' -and -not $ThroughRegistry) {
        Write-CommandFailure -Command $PSCmdlet -Message "RestartLabProgram needs -ThroughRegistry: the lab endpoint comes back at a new address, which a pure client told the old one never learns."
        return
    }
    $hyperVHost = if (-not $OnThisMachine) { Get-ConfiguredHyperVHost }
    if ($hyperVHost) { return Invoke-LabCommandOnHyperVHost -HyperVHost $hyperVHost -Command $PSCmdlet }

    $whatWasDoneToTheMachine = [Collections.Generic.List[string]]::new()
    function Mark([string]$whatWasDone) { $whatWasDoneToTheMachine.Add(('{0:HH:mm:ss.fff} >>>>>> {1}' -f (Get-Date), $whatWasDone)) }

    function Stop-LabProgramInGuest { Invoke-InLabGuest -ArgumentList $labProgramName { param($labProgram) Get-Process $labProgram | Stop-Process -Force } }

    $secondsOfConversation = $SecondsBeforeVanishing + $SecondsVanished + $SecondsAfterComingBack
    if ($ThroughRegistry) {
        $registryServerListensAt = "$(Get-LabHostAddress):$RegistryServerPort"
        Mark "starting the lab endpoint in the guest, told that its registry server will listen at $registryServerListensAt"
        $startedAt = C-VM-StartLabEndpoint -RegistryServerListensAt $registryServerListensAt -OnThisMachine
        Mark "the lab endpoint listens at $startedAt"
        $howToConverse = @('converse-through-registry', $registryServerListensAt)
    } else {
        $howToConverse = @('converse', $LabEndpointAddress)
    }

    $conversationOutput = Join-Path ([IO.Path]::GetTempPath()) "Compze\conversation-with-the-lab-endpoint-$([Guid]::NewGuid().ToString('N'))"
    New-Item -ItemType Directory -Path (Split-Path $conversationOutput -Parent) -Force | Out-Null
    try {
        $conversation = Start-Process $labProgram -ArgumentList ($howToConverse + @('--for', $secondsOfConversation) + $ConversationOptions) `
            -RedirectStandardOutput "$conversationOutput.log" -RedirectStandardError "$conversationOutput.errors.log" -PassThru -WindowStyle Hidden

        Start-Sleep -Seconds $SecondsBeforeVanishing
        switch ($Vanish) {
            'Pause'             { Mark 'pausing the machine'; Suspend-VM -Name $machine.Name; Mark 'the machine is paused' }
            'DisconnectNetwork' { Mark 'disconnecting the machine from the network'; Disconnect-VMNetworkAdapter -VMName $machine.Name; Mark 'the machine is disconnected' }
            'TurnOff'           { Mark 'turning the machine off'; Stop-VM -Name $machine.Name -TurnOff -Force; Mark 'the machine is off' }
            'KillLabProgram'    { Mark 'killing the lab program in the guest'; Stop-LabProgramInGuest; Mark 'the lab program is killed' }
            'RestartLabProgram' { Mark 'killing the lab program in the guest'; Stop-LabProgramInGuest; Mark 'the lab program is killed' }
        }
        Start-Sleep -Seconds $SecondsVanished
        switch ($Vanish) {
            'Pause'             { Mark 'resuming the machine'; Resume-VM -Name $machine.Name; Mark 'the machine is resumed' }
            'DisconnectNetwork' { Mark 'connecting the machine to the network again'; Connect-VMNetworkAdapter -VMName $machine.Name -SwitchName $machine.Switch; Mark 'the machine is connected again' }
            'TurnOff'           { Mark 'the machine stays off' }
            'KillLabProgram'    { Mark 'the lab program stays killed' }
            'RestartLabProgram' { Mark 'starting the lab program in the guest again'; $startedAt = C-VM-StartLabEndpoint -RegistryServerListensAt $registryServerListensAt -OnThisMachine; Mark "the lab endpoint listens at $startedAt" }
        }

        # A tuery asked just before the conversation's time is up may take the connect timeout to fail.
        if (-not $conversation.WaitForExit(($SecondsAfterComingBack + 90) * 1000)) {
            Mark 'the conversation had not ended 90 s after it should have: killing it'
            $conversation.Kill()
        }

        # One timeline, in time order. A line that starts with no time belongs to the line before it: a failure in full.
        $timeline = [Collections.Generic.List[object]]::new()
        foreach ($line in @(Get-Content "$conversationOutput.log")) {
            if ($line -match '^\d\d:\d\d:\d\d\.\d{3} ') { $timeline.Add([pscustomobject]@{ Time = $line.Substring(0, 12); Lines = [Collections.Generic.List[string]]@($line) }) }
            elseif ($timeline.Count -gt 0) { $timeline[$timeline.Count - 1].Lines.Add($line) }
        }
        foreach ($mark in $whatWasDoneToTheMachine) { $timeline.Add([pscustomobject]@{ Time = $mark.Substring(0, 12); Lines = [Collections.Generic.List[string]]@($mark) }) }
        if (-not $FailuresInFull) { $timeline = @($timeline | Where-Object { $_.Lines[0] -notmatch 'the first failure of that kind, in full:' }) }
        $timeline | Sort-Object Time -Stable | ForEach-Object { $_.Lines }

        $conversationErrors = Get-Content "$conversationOutput.errors.log" -ErrorAction SilentlyContinue
        if ($conversationErrors) {
            Write-CommandFailure -Command $PSCmdlet -Message "The conversation failed:`n$($conversationErrors -join "`n")"
            return
        }
        $global:LASTEXITCODE = 0
    } finally {
        Remove-Item "$conversationOutput.log", "$conversationOutput.errors.log" -ErrorAction SilentlyContinue
    }
}
