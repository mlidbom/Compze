function Get-CompzeCommands {
    <#
    .SYNOPSIS
    Lists all Compze module commands with their syntax
    
    .DESCRIPTION
    Displays all commands from the Compze module with their parameters/switches
    and synopsis in a formatted table view.
    
    .EXAMPLE
    Get-CompzeCommands
    Shows all Compze commands with their syntax and descriptions
    #>
    [CmdletBinding()]
    param()
    
    $commands = Get-Command -Module Compze | Where-Object { $_.Name -ne 'Get-CompzeCommands' } | Sort-Object Name
    
    $results = foreach ($cmd in $commands) {
        # Get the syntax - PowerShell formats this nicely
        $syntax = (Get-Command $cmd.Name -Syntax) -replace "`r`n", ' ' -replace '\s+', ' '
        
        # Get the synopsis
        $help = Get-Help $cmd.Name
        $synopsis = $help.Synopsis
        
        [PSCustomObject]@{
            Syntax = $syntax
            Synopsis = $synopsis
        }
    }
    
    # Display with Format-Table, wrapping the Synopsis column if needed
    $results | Format-Table -Property Syntax, Synopsis -Wrap
}
