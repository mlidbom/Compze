# Turnkey iteration step: stop old instance -> build -> relaunch detached -> capture -> tail log.
# Prints the capture path and the tail of agent.log. Read the printed PNG to see the result.
#
#   pwsh scripts/build-run-capture.ps1 -Csproj C:\path\App.csproj -Exe C:\path\bin\Debug\<tfm>\App.exe
param(
    [Parameter(Mandatory)][string]$Csproj,
    [Parameter(Mandatory)][string]$Exe,
    [string]$ProcName,                       # defaults to the exe base name
    [string]$Out = "capture.png",
    [int]$WarmupSec = 3,
    [string[]]$AppArgs = @('--harness')      # harness mode opens the overlay + capture pump; a no-arg launch is the real daemon
)
if (-not $ProcName) { $ProcName = [System.IO.Path]::GetFileNameWithoutExtension($Exe) }
$Dir = Split-Path $Exe -Parent

Stop-Process -Name $ProcName -Force -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 300

dotnet build $Csproj -c Debug -v minimal 2>&1 | Select-Object -Last 6
if ($LASTEXITCODE -ne 0) { Write-Error "build failed"; exit 1 }

Remove-Item (Join-Path $Dir "capture.png"),(Join-Path $Dir "capture.trigger") -ErrorAction SilentlyContinue
if ($AppArgs) { Start-Process -FilePath $Exe -ArgumentList $AppArgs | Out-Null } else { Start-Process -FilePath $Exe | Out-Null }
Start-Sleep -Seconds $WarmupSec

$png = & (Join-Path $PSScriptRoot "capture.ps1") -Dir $Dir -Out $Out
"--- capture: $png"
$log = Join-Path $Dir "agent.log"
if (Test-Path $log) { "--- agent.log (tail) ---"; Get-Content $log | Select-Object -Last 12 }
