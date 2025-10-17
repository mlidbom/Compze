# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Get-Commands {
    <#
    .SYNOPSIS
    Lists all Compze module commands
    
    .DESCRIPTION
    Displays all commands from the Compze module with their synopsis.
    Use -Syntax to show detailed syntax information.
    
    .PARAMETER Syntax
    Include detailed syntax information for each command
    
    .EXAMPLE
    C-Get-Commands
    Shows all Compze commands with their names and descriptions
    
    .EXAMPLE
    C-Get-Commands -Syntax
    Shows all Compze commands with their full syntax
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [switch]$Syntax
    )
    
    $commands = Get-Command -Module Compze | Where-Object { $_.Name -ne 'C-Get-Commands' } | Sort-Object Name
    
    if ($Syntax) {
        $results = foreach ($cmd in $commands) {
            # Get the syntax - PowerShell formats this nicely
            $syntaxText = (Get-Command $cmd.Name -Syntax) -replace "`r`n", ' ' -replace '\s+', ' '
            
            # Get the synopsis
            $help = Get-Help $cmd.Name
            $synopsis = $help.Synopsis
            
            [PSCustomObject]@{
                Syntax = $syntaxText
                Synopsis = $synopsis
            }
        }
        
        # Display with Format-Table, wrapping the Synopsis column if needed
        $results | Format-Table -Property Syntax, Synopsis -Wrap
    } else {
        $results = foreach ($cmd in $commands) {
            # Get the synopsis
            $help = Get-Help $cmd.Name
            $synopsis = $help.Synopsis
            
            [PSCustomObject]@{
                Name = $cmd.Name
                Synopsis = $synopsis
            }
        }
        
        # Display with Format-Table, wrapping the Synopsis column if needed
        $results | Format-Table -Property Name, Synopsis -Wrap
    }
}
