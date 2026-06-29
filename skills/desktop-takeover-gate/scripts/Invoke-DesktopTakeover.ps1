<#
.SYNOPSIS
    Gate a foreground "desktop takeover" run behind a permission prompt and a symmetric done notification.

.DESCRIPTION
    Shows a full-screen, always-on-top, all-desktops, brightly-coloured permission prompt with huge text ("about to run X
    in the foreground; you won't be able to use the computer; runs automatically in N seconds") and [OK]/[Cancel] plus an
    N-second auto-proceed countdown — deliberately impossible to miss. On OK (or timeout) it runs the command in the
    foreground and waits for it, then shows a matching green "done" notification that auto-closes. On Cancel it does NOT run
    the command and exits 64 — the unique "the user cancelled" signal.

    Each window is shown on a dedicated STA runspace (WinForms requires an STA thread; doing it that way makes the gate work
    whatever apartment the host PowerShell is in). The command is launched with Start-Process (ShellExecute), so it correctly
    starts uiAccess executables (which a plain CreateProcess refuses with ERROR_ELEVATION_REQUIRED) and gives them their own
    console.

.PARAMETER Description
    The human-readable name of the task, shown in both prompts (e.g. "the Deskmancer.ZOrderLab Z-order battery").

.PARAMETER FilePath
    The executable or command to run once the takeover is confirmed.

.PARAMETER ArgumentList
    Arguments passed to the command.

.PARAMETER CountdownSeconds
    Seconds the confirm prompt auto-proceeds after, and the done notice auto-closes after. Default 5.

.OUTPUTS
    Exit code: 64 when the user cancelled (command not run); otherwise the command's exit code (0 when it can't be read -
    judge success from whatever the command itself produced, not this code).
#>
[CmdletBinding()]
param(
   [Parameter(Mandatory)][string]$Description,
   [Parameter(Mandatory)][string]$FilePath,
   [string[]]$ArgumentList = @(),
   [int]$CountdownSeconds = 5
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

# A full-screen, always-on-top, all-desktops, brightly-coloured dialog with huge text and an N-second countdown, returned as
# the string 'OK' or 'Cancel'. ShowCancel adds a Cancel button (the confirm prompt); without it the dialog only counts down
# to close (the done notice). Run on a dedicated STA runspace: WinForms must be driven from an STA thread, and hosting it
# here makes the gate work no matter which apartment the calling PowerShell is in.
function Show-CountdownDialog {
   param([string]$Title, [string]$Message, [int]$Seconds, [string]$CountdownTemplate, [bool]$ShowCancel, [string]$BackColorName)

   $dialog = {
      param([string]$Title, [string]$Message, [int]$Seconds, [string]$CountdownTemplate, [bool]$ShowCancel, [string]$BackColorName)

      Add-Type -AssemblyName System.Windows.Forms
      Add-Type -AssemblyName System.Drawing
      Add-Type @"
using System;
using System.Runtime.InteropServices;
public static class GateNative {
   [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hWnd);
   [DllImport("user32.dll")] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
   [DllImport("user32.dll")] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
"@
      [System.Windows.Forms.Application]::EnableVisualStyles()

      $screen = [System.Windows.Forms.Screen]::PrimaryScreen.Bounds
      $width = $screen.Width
      $height = $screen.Height

      $form = New-Object System.Windows.Forms.Form
      $form.Text = $Title
      $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::None
      $form.StartPosition = [System.Windows.Forms.FormStartPosition]::Manual
      $form.Bounds = $screen
      $form.TopMost = $true
      $form.BackColor = [System.Drawing.Color]::FromName($BackColorName)

      $messageLabel = New-Object System.Windows.Forms.Label
      $messageLabel.Text = $Message
      $messageLabel.ForeColor = [System.Drawing.Color]::White
      $messageLabel.Font = New-Object System.Drawing.Font("Segoe UI", 30, [System.Drawing.FontStyle]::Bold)
      $messageLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
      $messageLabel.SetBounds([int]($width * 0.05), [int]($height * 0.14), [int]($width * 0.90), [int]($height * 0.44))
      $form.Controls.Add($messageLabel)

      $countdownLabel = New-Object System.Windows.Forms.Label
      $countdownLabel.ForeColor = [System.Drawing.Color]::White
      $countdownLabel.Font = New-Object System.Drawing.Font("Segoe UI", 22, [System.Drawing.FontStyle]::Bold)
      $countdownLabel.TextAlign = [System.Drawing.ContentAlignment]::MiddleCenter
      $countdownLabel.SetBounds([int]($width * 0.05), [int]($height * 0.60), [int]($width * 0.90), [int]($height * 0.10))
      $form.Controls.Add($countdownLabel)

      $buttonWidth = 320
      $buttonHeight = 96
      $gap = 64
      $buttonTop = [int]($height * 0.74)

      $okButton = New-Object System.Windows.Forms.Button
      $okButton.Text = if ($ShowCancel) { 'OK - run now' } else { 'Close now' }
      $okButton.Font = New-Object System.Drawing.Font("Segoe UI", 20, [System.Drawing.FontStyle]::Bold)
      $okButton.Size = New-Object System.Drawing.Size($buttonWidth, $buttonHeight)
      $okButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
      $form.AcceptButton = $okButton
      $form.Controls.Add($okButton)

      if ($ShowCancel) {
         $cancelButton = New-Object System.Windows.Forms.Button
         $cancelButton.Text = 'Cancel'
         $cancelButton.Font = New-Object System.Drawing.Font("Segoe UI", 20, [System.Drawing.FontStyle]::Bold)
         $cancelButton.Size = New-Object System.Drawing.Size($buttonWidth, $buttonHeight)
         $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
         $form.CancelButton = $cancelButton
         $form.Controls.Add($cancelButton)

         $startX = [int](($width - (2 * $buttonWidth + $gap)) / 2)
         $okButton.Location = New-Object System.Drawing.Point($startX, $buttonTop)
         $cancelButton.Location = New-Object System.Drawing.Point(($startX + $buttonWidth + $gap), $buttonTop)
      }
      else {
         $okButton.Location = New-Object System.Drawing.Point([int](($width - $buttonWidth) / 2), $buttonTop)
      }

      $script:remaining = $Seconds
      $countdownLabel.Text = ($CountdownTemplate -f $script:remaining)

      $timer = New-Object System.Windows.Forms.Timer
      $timer.Interval = 1000
      $timer.Add_Tick({
            $script:remaining--
            if ($script:remaining -le 0) {
               $timer.Stop()
               $form.DialogResult = [System.Windows.Forms.DialogResult]::OK
               $form.Close()
            }
            else {
               $countdownLabel.Text = ($CountdownTemplate -f $script:remaining)
            }
         }.GetNewClosure())

      $form.Add_Shown({
            # WS_EX_TOOLWINDOW (no shell application view) keeps it on every virtual desktop and out of the taskbar; then
            # force it to the very front so it cannot be missed.
            $GWL_EXSTYLE = -20
            $WS_EX_TOOLWINDOW = 0x80
            $current = [GateNative]::GetWindowLong($form.Handle, $GWL_EXSTYLE)
            [void][GateNative]::SetWindowLong($form.Handle, $GWL_EXSTYLE, ($current -bor $WS_EX_TOOLWINDOW))
            $form.Activate()
            [void][GateNative]::SetForegroundWindow($form.Handle)
         }.GetNewClosure())

      $timer.Start()
      $result = $form.ShowDialog()
      if ($result -eq [System.Windows.Forms.DialogResult]::OK) { 'OK' } else { 'Cancel' }
   }

   $runspace = [runspacefactory]::CreateRunspace()
   $runspace.ApartmentState = 'STA'
   $runspace.ThreadOptions = 'ReuseThread'
   $runspace.Open()
   $shell = [powershell]::Create()
   $shell.Runspace = $runspace
   [void]$shell.AddScript($dialog).
      AddArgument($Title).AddArgument($Message).AddArgument($Seconds).AddArgument($CountdownTemplate).AddArgument($ShowCancel).AddArgument($BackColorName)
   try { return ($shell.Invoke() | Select-Object -Last 1) }
   finally { $shell.Dispose(); $runspace.Dispose() }
}

$confirmed = Show-CountdownDialog `
   -Title 'About to take over your desktop' `
   -Message "I am about to run the following in the FOREGROUND on this computer:`n`n$Description`n`nWhile it runs you will NOT be able to use the computer. You will get a message when it is done." `
   -Seconds $CountdownSeconds `
   -CountdownTemplate 'Runs automatically in {0} seconds...   (Cancel to stop)' `
   -ShowCancel $true `
   -BackColorName 'DarkOrange'

if ($confirmed -ne 'OK') {
   Write-Output "DESKTOP-TAKEOVER CANCELLED BY USER: $Description"
   exit 64
}

$startArguments = @{ FilePath = $FilePath; Wait = $true; PassThru = $true }
if ($ArgumentList.Count -gt 0) { $startArguments['ArgumentList'] = $ArgumentList }

$process = Start-Process @startArguments
$exitCode = 0
try {
   # ExitCode is not always readable for a ShellExecute-launched process; fall back to 0 (the command's own output is the
   # real record of success, not this code - which only needs to distinguish "ran" from the 64 "cancelled" above).
   if ($null -ne $process) { try { $exitCode = $process.ExitCode } catch { $exitCode = 0 } }
}
finally {
   # Always tell the user their computer is theirs again - even if the run crashed.
   Show-CountdownDialog `
      -Title 'Desktop takeover finished' `
      -Message "The task taking over your computer is DONE:`n`n$Description`n`nYour computer is yours again." `
      -Seconds $CountdownSeconds `
      -CountdownTemplate 'This message closes in {0} seconds...' `
      -ShowCancel $false `
      -BackColorName 'SeaGreen' | Out-Null
}

exit $exitCode
