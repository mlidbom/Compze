function Kill-CompzeZombieDevProcesses {
    <#
    .SYNOPSIS
    Kills zombie dotnet.exe and conhost.exe build/test processes
    
    .DESCRIPTION
    Aggressively identifies and kills zombie build/test processes including:
    - Old MSBuild.dll node processes (running for > 2 minutes)
    - Old VBCSCompiler (Roslyn compiler server) processes
    - Old vstest.console (test runner) processes
    - Orphaned conhost.exe processes
    
    These zombie processes can prevent builds and cleans from succeeding by locking files.
    
    .PARAMETER WhatIf
    Shows what processes would be killed without actually killing them
    
    .PARAMETER Force
    Kills processes without prompting for confirmation
    
    .EXAMPLE
    Kill-CompzeZombieDevProcesses
    Shows zombie processes and prompts for confirmation before killing
    
    .EXAMPLE
    Kill-CompzeZombieDevProcesses -WhatIf
    Shows what processes would be killed without actually killing them
    
    .EXAMPLE
    Kill-CompzeZombieDevProcesses -Force
    Kills zombie processes without prompting for confirmation
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Force,
        
        [Parameter(Mandatory = $false)]
        [int]$AgeMinutes = 2
    )
    
    Write-Host "Scanning for zombie build/test processes (older than $AgeMinutes minutes)..." -ForegroundColor Cyan
    
    # Get all dotnet.exe processes
    $allDotnetProcesses = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
    
    # Filter dotnet processes that are zombie build/test processes
    $zombieDotnetProcesses = @()
    
    # Time threshold - processes older than specified minutes
    $zombieThreshold = (Get-Date).AddMinutes(-$AgeMinutes)
    
    foreach ($proc in $allDotnetProcesses) {
        try {
            $procInfo = Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)"
            $commandLine = $procInfo.CommandLine
            $parentPid = $procInfo.ParentProcessId
            
            if (-not $commandLine) {
                continue
            }
            
            # Check if this is a zombie build/test process - KILL THEM ALL if old enough
            $isZombieBuildProcess = $false
            $zombieType = ""
            
            # MSBuild node processes - kill if old
            if ($commandLine -like "*MSBuild.dll*" -and $commandLine -like "*/nodemode:*") {
                if ($proc.StartTime -lt $zombieThreshold) {
                    $isZombieBuildProcess = $true
                    $zombieType = "Old MSBuild node"
                }
            }
            # VBCSCompiler - Roslyn compiler server - kill if old
            elseif ($commandLine -like "*VBCSCompiler.dll*") {
                if ($proc.StartTime -lt $zombieThreshold) {
                    $isZombieBuildProcess = $true
                    $zombieType = "Old VBCSCompiler"
                }
            }
            # vstest.console - test runner processes - kill if old
            elseif ($commandLine -like "*vstest.console.dll*") {
                if ($proc.StartTime -lt $zombieThreshold) {
                    $isZombieBuildProcess = $true
                    $zombieType = "Old vstest.console"
                }
            }
            # buildhost - VS Code build processes - kill if old
            elseif ($commandLine -like "*buildhost*") {
                if ($proc.StartTime -lt $zombieThreshold) {
                    $isZombieBuildProcess = $true
                    $zombieType = "Old buildhost"
                }
            }
            # Orphaned processes (parent doesn't exist) - kill always
            elseif ($parentPid) {
                $parentProcess = Get-Process -Id $parentPid -ErrorAction SilentlyContinue
                if (-not $parentProcess) {
                    $isZombieBuildProcess = $true
                    $zombieType = "Orphaned (no parent)"
                }
            }
            
            if ($isZombieBuildProcess) {
                $zombieDotnetProcesses += [PSCustomObject]@{
                    ProcessName = "dotnet.exe"
                    ProcessId = $proc.Id
                    ParentProcessId = $parentPid
                    Type = $zombieType
                    CommandLine = $commandLine
                    StartTime = $proc.StartTime
                }
            }
        } catch {
            Write-Verbose "Could not process dotnet process $($proc.Id): $_"
        }
    }
    
    # Get all conhost.exe processes - be VERY aggressive
    $allConhostProcesses = Get-Process -Name "conhost" -ErrorAction SilentlyContinue
    $zombieConhostProcesses = @()
    
    foreach ($proc in $allConhostProcesses) {
        try {
            $parentPid = (Get-CimInstance Win32_Process -Filter "ProcessId = $($proc.Id)").ParentProcessId
            $isZombieConhost = $false
            $zombieReason = ""
            
            # Check if parent process exists
            if ($parentPid) {
                $parentProcess = Get-Process -Id $parentPid -ErrorAction SilentlyContinue
                
                # If parent doesn't exist, it's an orphan - KILL IT
                if (-not $parentProcess) {
                    $isZombieConhost = $true
                    $zombieReason = "Orphaned (no parent)"
                }
                # If parent is a zombie dotnet process we're killing, kill this too
                elseif ($zombieDotnetProcesses.ProcessId -contains $parentPid) {
                    $isZombieConhost = $true
                    $zombieReason = "Child of zombie dotnet"
                }
                # If parent is a dotnet.exe process, check if it's a build/test process
                elseif ($parentProcess.Name -eq "dotnet") {
                    $parentCommandLine = (Get-CimInstance Win32_Process -Filter "ProcessId = $parentPid").CommandLine
                    # If parent is a build/test process, kill the conhost too if it's old
                    if ($parentCommandLine -and (
                        $parentCommandLine -like "*MSBuild.dll*" -or
                        $parentCommandLine -like "*VBCSCompiler.dll*" -or
                        $parentCommandLine -like "*vstest.console.dll*" -or
                        $parentCommandLine -like "*buildhost*")) {
                        
                        if ($proc.StartTime -lt $zombieThreshold) {
                            $isZombieConhost = $true
                            $zombieReason = "Old conhost for build process"
                        }
                    }
                }
                # If the conhost itself is really old (older than age threshold), kill it regardless
                elseif ($proc.StartTime -lt $zombieThreshold) {
                    $isZombieConhost = $true
                    $zombieReason = "Old conhost (age > $AgeMinutes min)"
                }
            }
            else {
                # No parent PID found at all - definitely orphaned
                $isZombieConhost = $true
                $zombieReason = "Orphaned (no parent PID)"
            }
            
            if ($isZombieConhost) {
                $zombieConhostProcesses += [PSCustomObject]@{
                    ProcessName = "conhost.exe"
                    ProcessId = $proc.Id
                    ParentProcessId = $parentPid
                    Type = $zombieReason
                    CommandLine = "N/A"
                    StartTime = $proc.StartTime
                }
            }
        } catch {
            Write-Verbose "Could not check parent for conhost process $($proc.Id): $_"
        }
    }
    
    # IMPORTANT: Kill conhost processes FIRST, then dotnet processes
    # Conhost processes often hold file locks
    $allZombieProcesses = $zombieConhostProcesses + $zombieDotnetProcesses
    
    if ($allZombieProcesses.Count -eq 0) {
        Write-Host "No zombie processes found." -ForegroundColor Green
        return
    }
    
    Write-Host "`nFound $($allZombieProcesses.Count) zombie process(es):" -ForegroundColor Yellow
    Write-Host ""
    
    $allZombieProcesses | Format-Table -Property ProcessName, ProcessId, ParentProcessId, Type, StartTime, @{
        Label = "CommandLine"
        Expression = { 
            if ($_.CommandLine.Length -gt 50) { 
                $_.CommandLine.Substring(0, 47) + "..." 
            } else { 
                $_.CommandLine 
            } 
        }
    } -AutoSize | Out-Host
    
    if ($WhatIfPreference) {
        Write-Host "WhatIf: Would kill the above processes" -ForegroundColor Yellow
        return
    }
    
    $shouldKill = $Force
    if (-not $Force) {
        $response = Read-Host "`nDo you want to kill these processes? (Y/N)"
        $shouldKill = $response -eq 'Y' -or $response -eq 'y'
    }
    
    if ($shouldKill) {
        Write-Host "`nKilling zombie processes..." -ForegroundColor Yellow
        
        $killedCount = 0
        $failedCount = 0
        
        foreach ($zombieProc in $allZombieProcesses) {
            try {
                Stop-Process -Id $zombieProc.ProcessId -Force -ErrorAction Stop
                Write-Host "  Killed $($zombieProc.ProcessName) (PID: $($zombieProc.ProcessId))" -ForegroundColor Green
                $killedCount++
            } catch {
                Write-Warning "  Failed to kill $($zombieProc.ProcessName) (PID: $($zombieProc.ProcessId)): $_"
                $failedCount++
            }
        }
        
        Write-Host "`nSummary: Killed $killedCount process(es), Failed: $failedCount" -ForegroundColor $(if ($failedCount -eq 0) { 'Green' } else { 'Yellow' })
        
        if ($killedCount -gt 0) {
            Write-Host "You should now be able to build and clean successfully." -ForegroundColor Cyan
        }
    } else {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
    }
}
