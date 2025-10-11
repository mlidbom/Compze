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
    
    # Load the .psm1 directly to avoid manifest sync issues
    Import-Module (Join-Path $PSScriptRoot "..\Compze.psm1") -DisableNameChecking -Force -Global
    
    if (Test-Path $PROFILE) {
        . $PROFILE
    }
}
