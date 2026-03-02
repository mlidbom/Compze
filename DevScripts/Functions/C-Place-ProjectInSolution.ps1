# NOTE: Scripts that make changes should NOT log everything they do. They should only write output if something goes wrong.
function C-Place-ProjectInSolution {
    <#
    .SYNOPSIS
    Places a project in the correct folder structure within the solution file
    
    .DESCRIPTION
    Adds or moves a project within the solution folder structure to match its path.
    If the project doesn't exist in the solution, it will be added.
    This is purely a solution file organization update - it doesn't move any files.
    
    .PARAMETER ProjectName
    The name of the project to place in the solution
    
    .PARAMETER SolutionPath
    Path to the solution file (defaults to src\Compze.AllProjects.slnx)
    
    .EXAMPLE
    C-Place-ProjectInSolution Compze.Common.Configuration
    #>
    [CmdletBinding()]
    [Diagnostics.CodeAnalysis.SuppressMessageAttribute('PSUseApprovedVerbs', '')]
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectName,
        [string]$SolutionPath
    )
    
    if (-not $SolutionPath) { $SolutionPath = $script:CompzeSolutionPath }
    if (-not (Test-Path $SolutionPath)) { Write-Error "Solution file not found: $SolutionPath"; return }
    if (-not $SolutionPath.EndsWith('.slnx')) { Write-Error "Only works with .slnx files"; return }
    
    Ensure-ProjectIsInSolution -ProjectName $ProjectName -SolutionPath $SolutionPath
    
    Place-ProjectInCorrectSolutionFolder -ProjectName $ProjectName -SolutionPath $SolutionPath
}
