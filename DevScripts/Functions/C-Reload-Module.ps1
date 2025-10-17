# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Reload-Module {
    <#
    .SYNOPSIS
    Reloads the Compze PowerShell module
    
    .DESCRIPTION
    Force-reloads the Compze module to pick up any changes without restarting PowerShell.
    Also reloads your PowerShell profile if it exists.
    
    .EXAMPLE
    C-Reload-Module
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
