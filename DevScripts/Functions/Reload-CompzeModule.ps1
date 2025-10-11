function Reload-CompzeModule {
    <#
    .SYNOPSIS
    Reloads the Compze PowerShell module
    
    .DESCRIPTION
    Force-reloads the Compze module to pick up any changes without restarting PowerShell.
    Also reloads your PowerShell profile if it exists.
    
    .EXAMPLE
    Reload-CompzeModule
    Reloads the Compze module and your profile
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param()
    
    Import-Module (Join-Path $PSScriptRoot "..\Compze.psd1") -DisableNameChecking -Force -Global
    
    if (Test-Path $PROFILE) {
        . $PROFILE
    }
}
