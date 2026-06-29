<#
.SYNOPSIS
    Gate a foreground "desktop takeover" run behind a permission prompt and a symmetric done notification.

.DESCRIPTION
    Shows a topmost, all-desktops confirm prompt ("about to run X in the foreground; you won't be able to use the
    computer; runs automatically in N seconds") with [OK]/[Cancel] and an N-second auto-proceed countdown. On OK (or
    timeout) it runs the command in the foreground and waits for it, then shows a matching "done" notification that
    auto-closes. On Cancel it does NOT run the command and exits 64 - the unique "the user cancelled" signal.

    Each dialog is shown on a dedicated STA runspace, because WinForms requires an STA thread; doing it that way makes
    the gate work whatever apartment the host PowerShell is in (a plain `pwsh -File` child can be MTA). The command is
    launched with Start-Process (ShellExecute), so it correctly starts uiAccess executables (which a plain CreateProcess
    refuses with ERROR_ELEVATION_REQUIRED) and gives them their own console.

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

# A topmost, all-desktops dialog with an N-second countdown, returned as the string 'OK' or 'Cancel'. ShowCancel adds a
# Cancel button (the confirm prompt); without it the dialog only counts down to close (the done notice). It is run on a
# dedicated STA runspace: WinForms must be driven from an STA thread, and hosting it here makes the gate work no matter
# which apartment the calling PowerShell is in.
function Show-CountdownDialog {
   param([string]$Title, [string]$Message, [int]$Seconds, [string]$CountdownTemplate, [bool]$ShowCancel)

   $dialog = {
      param([string]$Title, [string]$Message, [int]$Seconds, [string]$CountdownTemplate, [bool]$ShowCancel)

      Add-Type -AssemblyName System.Windows.Forms
      Add-Type -AssemblyName System.Drawing
      [System.Windows.Forms.Application]::EnableVisualStyles()

      $form = New-Object System.Windows.Forms.Form
      $form.Text = $Title
      # FixedToolWindow carries WS_EX_TOOLWINDOW, which (having no shell application view) puts the window on EVERY virtual
      # desktop - so the prompt is seen wherever the user currently is, not only the desktop it was created on.
      $form.FormBorderStyle = [System.Windows.Forms.FormBorderStyle]::FixedToolWindow
      $form.StartPosition = [System.Windows.Forms.FormStartPosition]::CenterScreen
      $form.TopMost = $true
      $form.MinimizeBox = $false
      $form.MaximizeBox = $false
      $form.ClientSize = New-Object System.Drawing.Size(520, 188)

      $messageLabel = New-Object System.Windows.Forms.Label
      $messageLabel.Text = $Message
      $messageLabel.SetBounds(16, 16, 488, 96)
      $form.Controls.Add($messageLabel)

      $countdownLabel = New-Object System.Windows.Forms.Label
      $countdownLabel.SetBounds(16, 120, 488, 22)
      $form.Controls.Add($countdownLabel)

      $okButton = New-Object System.Windows.Forms.Button
      if ($ShowCancel) { $okButton.Text = 'OK' } else { $okButton.Text = 'Close now' }
      $okButton.DialogResult = [System.Windows.Forms.DialogResult]::OK
      $okButton.SetBounds(416, 150, 88, 28)
      $form.Controls.Add($okButton)
      $form.AcceptButton = $okButton

      if ($ShowCancel) {
         $cancelButton = New-Object System.Windows.Forms.Button
         $cancelButton.Text = 'Cancel'
         $cancelButton.DialogResult = [System.Windows.Forms.DialogResult]::Cancel
         $cancelButton.SetBounds(320, 150, 88, 28)
         $form.Controls.Add($cancelButton)
         $form.CancelButton = $cancelButton
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

      $form.Add_Shown({ $form.Activate() }.GetNewClosure())
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
      AddArgument($Title).AddArgument($Message).AddArgument($Seconds).AddArgument($CountdownTemplate).AddArgument($ShowCancel)
   try { return ($shell.Invoke() | Select-Object -Last 1) }
   finally { $shell.Dispose(); $runspace.Dispose() }
}

$confirmed = Show-CountdownDialog `
   -Title 'About to take over your desktop' `
   -Message "I am about to run the following in the foreground on this computer:`n`n    $Description`n`nWhile it runs you will not be able to use the computer. You will get a new message when it is done." `
   -Seconds $CountdownSeconds `
   -CountdownTemplate 'Runs automatically in {0} seconds...   (Cancel to stop)' `
   -ShowCancel $true

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
      -Message "The task taking over your computer is done:`n`n    $Description`n`nYour computer is yours again." `
      -Seconds $CountdownSeconds `
      -CountdownTemplate 'This message will close in {0} seconds...' `
      -ShowCancel $false | Out-Null
}

exit $exitCode
