# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.

function Get-AllProjectFiles {
    <#
    .SYNOPSIS
    Gets all .csproj files in the solution directory
    
    .DESCRIPTION
    Recursively finds all .csproj files in the solution directory
    
    .PARAMETER SolutionPath
    Path to the solution file
    
    .RETURNS
    Array of FileInfo objects for all .csproj files
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory = $true)]
        [string]$SolutionPath
    )
    
    if (-not (Test-Path $SolutionPath)) {
        Write-Error "Solution file not found: $SolutionPath"
        return @()
    }
    
    $solutionDir = Split-Path -Parent $SolutionPath
    $repoRoot = Split-Path -Parent $solutionDir
    return Get-CsprojFiles -Path $repoRoot
}
