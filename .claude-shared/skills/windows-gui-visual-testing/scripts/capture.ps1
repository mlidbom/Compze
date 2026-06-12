# Drive ONE self-capture against an already-running app instrumented with AgentHarness.
# Drops capture.trigger next to the exe, waits for capture.png to (re)appear, prints its path (optionally
# copies to a named file so successive captures don't overwrite each other).
#
#   pwsh scripts/capture.ps1 -Dir <exeDir>                      # -> <exeDir>/capture.png
#   pwsh scripts/capture.ps1 -Dir <exeDir> -Out frame1.png      # -> <exeDir>/frame1.png
param(
    [Parameter(Mandatory)][string]$Dir,
    [string]$Out = "capture.png",
    [int]$TimeoutSec = 6
)
$png = Join-Path $Dir "capture.png"
$trg = Join-Path $Dir "capture.trigger"
Remove-Item $png -ErrorAction SilentlyContinue
New-Item -ItemType File $trg | Out-Null
$deadline = (Get-Date).AddSeconds($TimeoutSec)
while (-not (Test-Path $png) -and (Get-Date) -lt $deadline) { Start-Sleep -Milliseconds 100 }
if (-not (Test-Path $png)) {
    Write-Error "No capture produced. Is the app running with AgentHarness.StartCapturePump(this)?"
    exit 1
}
$dest = Join-Path $Dir $Out
if ($Out -ne "capture.png") { Copy-Item $png $dest -Force }
Write-Output $dest
