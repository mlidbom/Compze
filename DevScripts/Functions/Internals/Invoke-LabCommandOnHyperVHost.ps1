function Invoke-LabCommandOnHyperVHost {
    <#
    .SYNOPSIS
    Runs a C-VM-* command on the Hyper-V host instead of here, and returns what it returns

    .DESCRIPTION
    For a lab machine that runs on another machine's Hyper-V (Get-ConfiguredHyperVHost). PowerShell Direct, the Hyper-V
    cmdlets and the Default Switch's network reach a virtual machine only from the machine it runs on, so the whole
    command runs there as it would run here, the host's side of a conversation with the guest included:

      - What the C-VM-* commands read from the repository is copied to the host first, laid out as it is here, in
        <lab folder>\hyper-v-host-scripts\<worktree>: the DevScripts, src\Directory.Build.props, and the lab program's
        project file with its latest build. Only what differs is copied.
      - The command runs there in PowerShell 7, over WinRM, with the parameters it was given here, and -OnThisMachine.
        What it writes there is shown here as it comes, apart from its output.
      - Its output is returned here as objects, such as the address C-VM-StartLabEndpoint returns, and its failure there
        is the command's failure here.

    An address the command returns, such as the lab endpoint's, is on the network the host shares with its guest, which
    only the host reaches. A command that asks before it acts, such as C-VM-RemoveLabMachine, asks here before this runs
    it, and is told there not to ask again.

    .PARAMETER HyperVHost
    The host, as Get-ConfiguredHyperVHost returns it.

    .PARAMETER Command
    The C-VM-* command's $PSCmdlet: which command it is, and the parameters it was given.
    #>
    param(
        [Parameter(Mandatory)]
        $HyperVHost,

        [Parameter(Mandatory)]
        [System.Management.Automation.PSCmdlet]$Command
    )

    $ErrorActionPreference = 'Stop'
    $machine = Get-LabMachine
    $commandName = $Command.MyInvocation.MyCommand.Name
    $hostScriptsFolder = Join-Path $machine.LabFolder "hyper-v-host-scripts\$(Split-Path $script:CompzeRoot -Leaf)"

    # This machine's path in the repository, as the host has it in its copy.
    function Get-HostCopyPath([string]$pathInTheRepository) {
        Join-Path $hostScriptsFolder ([IO.Path]::GetRelativePath($script:CompzeRoot, $pathInTheRepository))
    }

    function Copy-FileToHostCopy([string]$file) {
        $destination = Get-PathThroughTheHyperVHostShare -HyperVHost $HyperVHost -PathOnTheHost (Get-HostCopyPath $file)
        New-Item -ItemType Directory -Path (Split-Path $destination -Parent) -Force | Out-Null
        Copy-Item -LiteralPath $file -Destination $destination -Force
    }

    function Copy-WhatTheLabCommandsReadToTheHost {
        $devScripts = Join-Path $script:CompzeRoot 'DevScripts'
        Sync-FolderToHyperVHost -HyperVHost $HyperVHost -Folder $devScripts -PathOnTheHost (Get-HostCopyPath $devScripts) -FileFilter '*.ps1', '*.psm1' -ExcludedFolders 'bin', 'obj'
        Copy-FileToHostCopy (Join-Path $script:CompzeRoot 'src\Directory.Build.props')

        $labProgramProject = Join-Path $script:CompzeRoot 'test\Compze.Tests.AcrossMachines.Lab\Compze.Tests.AcrossMachines.Lab.csproj'
        Copy-FileToHostCopy $labProgramProject
        $builtLabProgram = Get-BuiltOutputFolder -ProjectFile $labProgramProject
        if ($builtLabProgram) { Sync-FolderToHyperVHost -HyperVHost $HyperVHost -Folder $builtLabProgram -PathOnTheHost (Get-HostCopyPath $builtLabProgram) }
    }

    # The command as the host runs it: the parameters travel as PowerShell literals inside it, and its output is written to
    # $resultFile for this machine to read back. A terminating error ends it with exit code 1; a failure the command
    # reports itself (Write-CommandFailure) sets the exit code it ends with.
    function Get-CommandForTheHost([string]$resultFile) {
        $ownParameters = $Command.MyInvocation.BoundParameters.GetEnumerator() | Where-Object {
            $_.Key -notin [System.Management.Automation.PSCmdlet]::CommonParameters -and
            $_.Key -notin [System.Management.Automation.PSCmdlet]::OptionalCommonParameters -and
            $_.Key -ne 'OnThisMachine'
        }
        $parameterLiterals = @($ownParameters | ForEach-Object { "$($_.Key) = $(ConvertTo-PowerShellLiteral $_.Value)" }) + 'OnThisMachine = $true'
        if ($Command.MyInvocation.MyCommand.Parameters.ContainsKey('Confirm')) { $parameterLiterals += 'Confirm = $false' }

        @(
            'trap { [Console]::Error.WriteLine(($_ | Out-String).Trim()); exit 1 }'
            "Import-Module $(ConvertTo-PowerShellLiteral (Join-Path $hostScriptsFolder 'DevScripts\Compze.psm1')) -DisableNameChecking"
            "`$parameters = @{ $($parameterLiterals -join '; ') }"
            "`$output = @($commandName @parameters)"
            "`$exitCode = `$global:LASTEXITCODE"
            "New-Item -ItemType Directory -Path $(ConvertTo-PowerShellLiteral (Split-Path $resultFile -Parent)) -Force | Out-Null"
            "`$output | Export-Clixml -LiteralPath $(ConvertTo-PowerShellLiteral $resultFile)"
            'exit $exitCode'
        ) -join "`n"
    }

    # Runs it in PowerShell 7 on the host, shows what it writes as it comes, and returns its exit code. WinRM's session on the
    # host is Windows PowerShell, which would lose the double quotes in a pwsh command line, hence the encoded command.
    # -OutputFormat Text: else pwsh writes its standard error as serialized XML, which Windows PowerShell then fails to read.
    function Invoke-CommandOnTheHost([string]$commandForTheHost) {
        $encodedCommand = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($commandForTheHost))
        $session = New-PSSession -ComputerName $HyperVHost.ComputerName -Credential $HyperVHost.Credential
        try {
            Invoke-Command -Session $session -ArgumentList $encodedCommand -ScriptBlock {
                param($encodedCommand)
                $ErrorActionPreference = 'Continue' # what the command writes to its standard error is shown, and doesn't stop it
                & pwsh -NoProfile -NonInteractive -OutputFormat Text -EncodedCommand $encodedCommand 2>&1 | ForEach-Object { "$_" }
            } | Out-Host
            Invoke-Command -Session $session -ScriptBlock { $LASTEXITCODE }
        } finally {
            Remove-PSSession $session
        }
    }

    Copy-WhatTheLabCommandsReadToTheHost
    $resultFile = Join-Path $machine.LabFolder "hyper-v-host-results\$commandName-$([Guid]::NewGuid().ToString('N')).clixml"
    $exitCode = Invoke-CommandOnTheHost (Get-CommandForTheHost $resultFile)

    $resultFileThroughTheShare = Get-PathThroughTheHyperVHostShare -HyperVHost $HyperVHost -PathOnTheHost $resultFile
    if (Test-Path -LiteralPath $resultFileThroughTheShare) {
        Import-Clixml -LiteralPath $resultFileThroughTheShare
        Remove-Item -LiteralPath $resultFileThroughTheShare
    }
    if ($exitCode -ne 0) {
        Write-CommandFailure -Command $Command -Message "$commandName failed on the Hyper-V host $($HyperVHost.ComputerName), with exit code ${exitCode}: what it wrote there is above."
        return
    }
    $global:LASTEXITCODE = 0
}
