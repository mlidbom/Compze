function Reload-Profile {
    <#
    .SYNOPSIS
    Reloads the PowerShell profile
    
    .DESCRIPTION
    Reloads your PowerShell profile without restarting the shell.
    Also force-reloads the Compze module to pick up any changes.
    
    .EXAMPLE
    Reload-Profile
    Reloads your profile and the Compze module
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    Import-Module (Join-Path $PSScriptRoot "..\Compze.psd1") -DisableNameChecking -Force -Global
    
    if (Test-Path $PROFILE) {
        . $PROFILE
    }
}
