function C-Kill-ZombieDevProcesses {
    <#
    .SYNOPSIS
    Kills hung Compze test executables that are locking files
    
    .DESCRIPTION
    Finds and kills hung Compze test executables (*.Tests.*.exe) that are preventing
    builds and cleans from succeeding by locking files.
    
    Only kills test executables from the Compze workspace - does NOT touch dotnet.exe,
    conhost.exe, or other system processes.
    
    .PARAMETER WhatIf
    Shows what processes would be killed without actually killing them
    
    .PARAMETER Force
    Kills processes without prompting for confirmation
    
    .EXAMPLE
    C-Kill-ZombieDevProcesses
    Shows hung test processes and prompts for confirmation before killing
    
    .EXAMPLE
    C-Kill-ZombieDevProcesses -WhatIf
    Shows what processes would be killed without actually killing them
    
    .EXAMPLE
    C-Kill-ZombieDevProcesses -Force
    Kills hung test processes without prompting for confirmation
    #>
    [CmdletBinding(SupportsShouldProcess)]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Force
    )
    
    Write-Host "Scanning for hung Compze test executables..." -ForegroundColor Cyan
    
    $compzeRoot = $script:CompzeRoot
    $zombieProcesses = @()
    
    # Find ALL processes
    $allProcesses = Get-Process -ErrorAction SilentlyContinue
    
    foreach ($proc in $allProcesses) {
        try {
            # Only look for Compze test executables
            if ($proc.Path -and 
                $proc.Path -like "*\Compze\*" -and 
                ($proc.Path -like "*.Tests.*.exe" -or $proc.Name -like "Compze.Tests.*")) {
                
                $zombieProcesses += [PSCustomObject]@{
                    ProcessName = $proc.Name
                    ProcessId = $proc.Id
                    Path = $proc.Path
                    StartTime = $proc.StartTime
                    Age = if ($proc.StartTime) { 
                        [Math]::Round(((Get-Date) - $proc.StartTime).TotalMinutes, 1) 
                    } else { 
                        "Unknown" 
                    }
                }
            }
        } catch {
            Write-Verbose "Could not process $($proc.Id): $_"
        }
    }
    
    if ($zombieProcesses.Count -eq 0) {
        Write-Host "No hung Compze test processes found." -ForegroundColor Green
        return
    }
    
    Write-Host "`nFound $($zombieProcesses.Count) hung test process(es):" -ForegroundColor Yellow
    Write-Host ""
    
    $zombieProcesses | Format-Table -Property ProcessName, ProcessId, @{
        Label = "Age (min)"
        Expression = { $_.Age }
    }, StartTime, Path -AutoSize | Out-Host
    
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
        Write-Host "`nKilling hung test processes..." -ForegroundColor Yellow
        
        $killedCount = 0
        $failedCount = 0
        
        foreach ($zombieProc in $zombieProcesses) {
            try {
                Stop-Process -Id $zombieProc.ProcessId -Force -ErrorAction Stop
                Write-Host "  Killed $($zombieProc.ProcessName) (PID: $($zombieProc.ProcessId), Age: $($zombieProc.Age) min)" -ForegroundColor Green
                $killedCount++
            } catch {
                Write-Warning "  Failed to kill $($zombieProc.ProcessName) (PID: $($zombieProc.ProcessId)): $_"
                $failedCount++
            }
        }
        
        Write-Host "`nSummary: Killed $killedCount test process(es), Failed: $failedCount" -ForegroundColor $(if ($failedCount -eq 0) { 'Green' } else { 'Yellow' })
        
        if ($killedCount -gt 0) {
            Write-Host "Killed the hung test processes. Other zombie dotnet/conhost processes should clean up automatically." -ForegroundColor Cyan
            Write-Host "You should now be able to build and clean successfully." -ForegroundColor Cyan
        }
    } else {
        Write-Host "Operation cancelled." -ForegroundColor Yellow
    }
}
